using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.AvaloniaUI.Postprocessing;
using MeshEditor.Common.Logging;
using MeshEditor.LayerManager.Import;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.AzureStorage;
using MeshEditor.SolutionManager.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Remote solutions in Azure Blob Storage of the user signed in with a Microsoft account. The sign-in is replaced by a test connection
	/// (the real one needs a Microsoft account). The tests that need a storage account run against the Azurite emulator
	/// (e.g. "npx azurite-blob --inMemoryPersistence --skipApiVersionCheck") or against the account given by the environment
	/// variable MESHEDITOR_TEST_AZURE_STORAGE_CONNECTION_STRING; they are ignored when neither is available.
	/// </summary>
	public class RemoteSolutionsTests
	{
		private const string TestConnectionStringEnvironmentVariable = "MESHEDITOR_TEST_AZURE_STORAGE_CONNECTION_STRING";
		private const string AzuriteConnectionString = "UseDevelopmentStorage=true";

		private static readonly RemoteUser Alice = new RemoteUser { Id = "00000000-0000-0000-0000-00000000a11c", Name = "alice@example.com" };
		private static readonly RemoteUser Bob = new RemoteUser { Id = "00000000-0000-0000-0000-000000000b0b", Name = "bob@example.com" };

		private static string testDataPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

		private readonly List<Window> openedWindows = new List<Window>();
		private string tempDirectory;
		private string originalConfigurationDirectory;
		private IRemoteStorageConnection originalConnection;
		private Func<CancellationToken, MemoryLogger, Task<IEnumerable<ISolutionInfo>>> originalEnumerateRemoteSolutions;
		private Func<string, IProgress<RemoteOperationProgress>, CancellationToken, MemoryLogger, Task<ISolutionInfo>> originalUploadLocalSolutionToRemote;
		private Func<int, IProgress<RemoteOperationProgress>, CancellationToken, MemoryLogger, Task> originalDeleteRemoteSolution;

		[SetUp]
		public void SetUp()
		{
			openedWindows.Clear();
			Window.WindowOpenedEvent.AddClassHandler<Window>((w, e) => openedWindows.Add(w), RoutingStrategies.Direct);
			tempDirectory = Path.Combine(Path.GetTempPath(), "MeshEditorTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDirectory);
			// never read or write the token cache of the real user configuration
			originalConfigurationDirectory = MeshEditor.Common.ConfigurationManager.ConfigurationDirectory;
			MeshEditor.Common.ConfigurationManager.ConfigurationDirectory = Path.Combine(tempDirectory, "config");
			originalConnection = SolutionHub.RemoteStorageConnection;
			originalEnumerateRemoteSolutions = SolutionBrowserDialog.EnumerateRemoteSolutionsAsync;
			originalUploadLocalSolutionToRemote = SolutionBrowserDialog.UploadLocalSolutionToRemoteAsync;
			originalDeleteRemoteSolution = SolutionBrowserDialog.DeleteRemoteSolutionAsync;
		}

		[TearDown]
		public void TearDown()
		{
			SolutionHub.RemoteStorageConnection = originalConnection;
			SolutionBrowserDialog.EnumerateRemoteSolutionsAsync = originalEnumerateRemoteSolutions;
			SolutionBrowserDialog.UploadLocalSolutionToRemoteAsync = originalUploadLocalSolutionToRemote;
			SolutionBrowserDialog.DeleteRemoteSolutionAsync = originalDeleteRemoteSolution;
			MeshEditor.Common.ConfigurationManager.ConfigurationDirectory = originalConfigurationDirectory;
			foreach (var window in openedWindows.Where(w => w.IsVisible).ToList())
				window.Close();
			try
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
			catch (IOException)
			{ }
		}

		#region Test connection

		/// <summary>
		/// Replaces the Microsoft account sign-in: the signed-in user is set by the test, the storage is accessed with a connection string.
		/// </summary>
		private class TestStorageConnection : IRemoteStorageConnection
		{
			public string ConnectionString { get; set; }
			public RemoteUser User { get; set; }
			public Func<Func<string, Task>, CancellationToken, Task<RemoteUser>> InteractiveSignIn { get; set; }

			public bool IsConfigured => true;
			public Task<RemoteUser> GetSignedInUserAsync(CancellationToken cancellationToken) => Task.FromResult(User);
			public async Task<RemoteUser> SignInInteractiveAsync(Func<string, Task> showDeviceCodeMessage, CancellationToken cancellationToken) => User = await InteractiveSignIn(showDeviceCodeMessage, cancellationToken);
			public Task<RemoteUser> SignInWithDeviceCodeAsync(Func<string, Task> showDeviceCodeMessage, CancellationToken cancellationToken) => throw new NotSupportedException();
			public Task SignOutAsync()
			{
				User = null;
				return Task.CompletedTask;
			}
			public BlobServiceClient CreateServiceClient(RemoteUser user)
			{
				Assert.That(user?.Id, Is.EqualTo(User?.Id), "storage is accessed as the signed-in user");
				return new BlobServiceClient(ConnectionString ?? throw new InvalidOperationException("no test storage"));
			}
		}

		private class TestSolutionInfo : ISolutionInfo
		{
			public int Id { get; set; }
			public string ProjectName { get; set; }
			public string Location { get; set; }
		}

		/// <summary>
		/// Progress reported on the reporting thread (<see cref="Progress{T}"/> posts it later).
		/// </summary>
		private class SynchronousProgress<T> : IProgress<T>
		{
			private readonly Action<T> report;
			public SynchronousProgress(Action<T> report) => this.report = report;
			public void Report(T value) => report(value);
		}

		/// <summary>
		/// Test connection to the test storage account (containers are created), ignores the test if there is no test storage.
		/// </summary>
		private static async Task<TestStorageConnection> useTestStorageAccountAsync(RemoteUser signedInUser)
		{
			string connectionString = Environment.GetEnvironmentVariable(TestConnectionStringEnvironmentVariable);
			if (string.IsNullOrWhiteSpace(connectionString))
			{
				if (!isAzuriteRunning())
					Assert.Ignore($"No test storage: start Azurite or set {TestConnectionStringEnvironmentVariable}.");
				connectionString = AzuriteConnectionString;
			}
			// the containers are created by the administrator, together with the access rights of the users
			var serviceClient = new BlobServiceClient(connectionString);
			var configuration = new AzureBlobStorageConfiguration();
			foreach (string container in new[] { configuration.SolutionsBlobContainerName, configuration.LayersBlobContainerName, configuration.ResultsBlobContainerName })
				await serviceClient.GetBlobContainerClient(container).CreateIfNotExistsAsync();

			var connection = new TestStorageConnection { ConnectionString = connectionString, User = signedInUser };
			SolutionHub.RemoteStorageConnection = connection;
			return connection;
		}

		private static bool isAzuriteRunning()
		{
			try
			{
				using var client = new TcpClient();
				return client.ConnectAsync("127.0.0.1", 10000).Wait(TimeSpan.FromSeconds(1)) && client.Connected;
			}
			catch (Exception)
			{
				return false;
			}
		}

		#endregion

		#region Helpers

		private string importLocalSolution(string solutionName = "props")
		{
			var analysisResults = new[]
			{
				new AnalysisResult
				{
					TimeStep = null,
					MeshRecordNames = new[] { testDataPath("props.msh") },
					DataRecordNames = new[] { testDataPath("props.res") },
				}
			};
			var solutionHub = SolutionHub.CreateNewLocal(Path.Combine(tempDirectory, solutionName), analysisResults, solutionName);
			solutionHub.Import(Enumerable.Empty<decimal>(), new string[0], "Nearest");
			return solutionHub.GetSolutionDescription().Location;
		}

		private static async Task waitUntil(Func<bool> condition, string description, int timeoutSeconds = 60)
		{
			var timeout = DateTime.UtcNow.AddSeconds(timeoutSeconds);
			while (!condition())
			{
				if (DateTime.UtcNow > timeout)
					Assert.Fail($"timeout: {description}");
				Dispatcher.UIThread.RunJobs();
				await Task.Delay(20);
			}
			Dispatcher.UIThread.RunJobs();
		}

		private async Task<T> waitForWindow<T>(Func<T, bool> predicate = null) where T : Window
		{
			T window = null;
			await waitUntil(() => (window = openedWindows.OfType<T>().LastOrDefault(w => w.IsVisible && (predicate == null || predicate(w)))) != null, $"{typeof(T).Name} is opened");
			return window;
		}

		private static void click(Control control) => control.RaiseEvent(new RoutedEventArgs(control is MenuItem ? MenuItem.ClickEvent : Button.ClickEvent));

		private static Button findButton(Window window, string name) => window.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name);

		private static BlobClient getSolutionBlob(BlobServiceClient serviceClient, RemoteUser user, int solutionId) =>
			serviceClient.GetBlobContainerClient("fea-solutions").GetBlobClient($"{user.Id}/{solutionId}.solution.json");

		/// <summary>
		/// Folder of the layer blobs of the remote solution, as recorded in the solution blob.
		/// </summary>
		private static async Task<string> getLayerFolderAsync(BlobServiceClient serviceClient, RemoteUser user, int solutionId)
		{
			var solutionJson = JObject.Parse((await getSolutionBlob(serviceClient, user, solutionId).DownloadContentAsync()).Value.Content.ToString());
			string layerFolder = solutionJson.Value<string>("LayerFolder");
			return layerFolder == null ? user.Id : $"{user.Id}/{layerFolder}";
		}

		private static async Task<SolutionBrowserDialog> showSolutionBrowserOnRemoteTab()
		{
			var dialog = new SolutionBrowserDialog();
			dialog.Show();
			await dialog.LoadingTask;
			Dispatcher.UIThread.RunJobs();
			dialog.tabControl.SelectedItem = dialog.tabPageRemoteSolutions;
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.RemoteLoadingTask, Is.Not.Null, "remote solutions are loaded when the tab is opened");
			await dialog.RemoteLoadingTask;
			Dispatcher.UIThread.RunJobs();
			return dialog;
		}

		#endregion

		[Test]
		public async Task Remote_storage_needs_account_url_tenant_and_client_in_configuration()
		{
			var incomplete = new MicrosoftAccountStorageConnection(new AzureBlobStorageConfiguration { AccountUrl = "https://account.blob.core.windows.net", TenantId = Guid.NewGuid().ToString() });
			Assert.That(incomplete.IsConfigured, Is.False);
			Assert.That(await incomplete.GetSignedInUserAsync(CancellationToken.None), Is.Null);

			// configured: the token cache in the secure storage of the operating system is empty, nobody is signed in
			var complete = new MicrosoftAccountStorageConnection(new AzureBlobStorageConfiguration { AccountUrl = "https://account.blob.core.windows.net", TenantId = Guid.NewGuid().ToString(), ClientId = Guid.NewGuid().ToString() });
			Assert.That(complete.IsConfigured, Is.True);
			if (OperatingSystem.IsWindows()) // on Linux and macOS the cache is the keyring of the user, shared with the real application
				Assert.That(await complete.GetSignedInUserAsync(CancellationToken.None), Is.Null);
			Assert.Throws<RemoteSignInRequiredException>(() => complete.CreateServiceClient(user: null));
		}

		[AvaloniaTest]
		public async Task Remote_solutions_tab_is_hidden_when_remote_storage_is_not_configured()
		{
			SolutionHub.RemoteStorageConnection = new MicrosoftAccountStorageConnection(new AzureBlobStorageConfiguration());
			var dialog = new SolutionBrowserDialog();
			dialog.Show();
			await dialog.LoadingTask;
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.tabPageRemoteSolutions.IsVisible, Is.False);
			Assert.That(dialog.buttonUploadLocalSolution.IsVisible, Is.False);
			Assert.That(dialog.RemoteLoadingTask, Is.Null, "remote solutions are not loaded");
			dialog.Close();
		}

		[AvaloniaTest]
		public async Task Remote_solutions_tab_signs_in_lists_solutions_of_the_user_and_signs_out()
		{
			var connection = new TestStorageConnection { User = null, InteractiveSignIn = (showDeviceCodeMessage, cancellationToken) => Task.FromResult(Alice) };
			SolutionHub.RemoteStorageConnection = connection;
			int enumerations = 0;
			SolutionBrowserDialog.EnumerateRemoteSolutionsAsync = (cancellationToken, logger) =>
			{
				enumerations++;
				return Task.FromResult<IEnumerable<ISolutionInfo>>(new[]
				{
					new TestSolutionInfo { Id = 7, ProjectName = "props", Location = $"https://account.blob.core.windows.net/fea-solutions/{Alice.Id}/7.solution.json" },
					new TestSolutionInfo { Id = 8, ProjectName = "props", Location = $"https://account.blob.core.windows.net/fea-solutions/{Alice.Id}/8.solution.json" },
				});
			};

			var dialog = await showSolutionBrowserOnRemoteTab();
			Assert.That(dialog.tabPageRemoteSolutions.IsVisible && dialog.buttonUploadLocalSolution.IsVisible, Is.True);

			// nobody signed in: nothing is listed
			Assert.That(dialog.labelRemoteUser.Text, Is.EqualTo("Not signed in"));
			Assert.That(dialog.buttonSignIn.IsVisible && !dialog.buttonSignOut.IsVisible, Is.True);
			Assert.That(dialog.remoteSolutionListView.Notification, Does.Contain("Sign in"));
			Assert.That(enumerations, Is.Zero);
			Assert.That(dialog.buttonRefreshRemoteSolutions.IsEnabled, Is.False);

			// sign in: the solutions of the user are listed
			click(dialog.buttonSignIn);
			await waitUntil(() => dialog.RemoteUser != null && enumerations == 1, "signed in and solutions listed");
			await dialog.RemoteLoadingTask;
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.labelRemoteUser.Text, Is.EqualTo("Signed in as alice@example.com"));
			Assert.That(!dialog.buttonSignIn.IsVisible && dialog.buttonSignOut.IsVisible, Is.True);
			var items = dialog.remoteSolutionListView.listView.ItemsSource.Cast<ISolutionInfo>().ToList();
			Assert.That(items.Select(s => s.Id), Is.EqualTo(new[] { 7, 8 }));

			dialog.remoteSolutionListView.listView.SelectedItem = items[1];
			Assert.That(dialog.buttonOk.IsEnabled && dialog.buttonDeleteRemoteSolution.IsEnabled, Is.True);
			Assert.That(dialog.SolutionLocation, Is.EqualTo(SolutionBrowserDialog.SolutionLocationType.Remote));
			Assert.That(dialog.RemoteSolutionId, Is.EqualTo(8));

			// sign out: the list is cleared
			click(dialog.buttonSignOut);
			await waitUntil(() => dialog.RemoteUser == null, "signed out");
			Assert.That(connection.User, Is.Null);
			Assert.That(dialog.labelRemoteUser.Text, Is.EqualTo("Not signed in"));
			Assert.That(dialog.remoteSolutionListView.listView.ItemsSource.Cast<object>(), Is.Empty);
			Assert.That(dialog.buttonOk.IsEnabled, Is.False);
			Assert.That(dialog.RemoteSolutionId, Is.Null);
			dialog.Close();
		}

		[AvaloniaTest]
		public async Task Device_code_is_shown_when_no_web_browser_can_be_opened()
		{
			const string deviceCodeMessage = "To sign in, use a web browser to open the page https://microsoft.com/devicelogin and enter the code TESTCODE to authenticate.";
			var signInCompletion = new TaskCompletionSource<RemoteUser>();
			var connection = new TestStorageConnection
			{
				User = null,
				InteractiveSignIn = async (showDeviceCodeMessage, cancellationToken) =>
				{
					await Task.Run(() => showDeviceCodeMessage(deviceCodeMessage)); // MSAL calls it from a background thread
					return await signInCompletion.Task;
				},
			};
			SolutionHub.RemoteStorageConnection = connection;
			SolutionBrowserDialog.EnumerateRemoteSolutionsAsync = (cancellationToken, logger) => Task.FromResult(Enumerable.Empty<ISolutionInfo>());

			var dialog = await showSolutionBrowserOnRemoteTab();
			click(dialog.buttonSignIn);
			var deviceCodeWindow = await waitForWindow<Window>(w => w.Title == "Sign in with Microsoft account");
			var texts = deviceCodeWindow.GetVisualDescendants().OfType<SelectableTextBlock>().Select(t => t.Text).ToList();
			Assert.That(texts, Does.Contain(deviceCodeMessage));
			Assert.That(dialog.buttonSignIn.Content, Is.EqualTo("Cancel sign-in"));

			signInCompletion.SetResult(Bob);
			await waitUntil(() => dialog.RemoteUser != null, "signed in");
			await waitUntil(() => !deviceCodeWindow.IsVisible, "device code window is closed");
			Assert.That(dialog.labelRemoteUser.Text, Is.EqualTo("Signed in as bob@example.com"));
			dialog.Close();
		}

		[AvaloniaTest]
		public async Task Upload_shows_progress_disables_the_dialog_and_can_be_cancelled()
		{
			SolutionHub.RemoteStorageConnection = new TestStorageConnection { User = Alice };
			string localSolutionFileName = importLocalSolution();
			IProgress<RemoteOperationProgress> uploadProgress = null;
			CancellationToken uploadCancellation = default;
			SolutionBrowserDialog.UploadLocalSolutionToRemoteAsync = async (solutionFileName, progress, cancellationToken, logger) =>
			{
				Assert.That(Path.GetFullPath(solutionFileName), Is.EqualTo(Path.GetFullPath(localSolutionFileName)));
				uploadProgress = progress;
				uploadCancellation = cancellationToken;
				await Task.Delay(Timeout.Infinite, cancellationToken); // until cancelled
				return null;
			};
			string originalDirectory = SolutionHub.GetLocalStorageDefaultDirectory();
			SolutionHub.SetLocalStorageDefaultDirectory(tempDirectory);
			try
			{
				var dialog = new SolutionBrowserDialog();
				dialog.Show();
				await dialog.LoadingTask;
				Dispatcher.UIThread.RunJobs();
				dialog.localSolutionListView.listView.SelectedItem = dialog.localSolutionListView.listView.ItemsSource.Cast<object>().Single();
				click(dialog.buttonUploadLocalSolution);

				var progressView = await waitForWindow<ProgressViewDialog>();
				Assert.That(progressView.Title, Is.EqualTo("Uploading solution 'props'"));
				Assert.That(progressView.progressBar.IsIndeterminate, Is.True, "nothing is reported yet");
				Assert.That(dialog.tabControl.IsEffectivelyEnabled, Is.False, "the tabs can't be switched during the upload");
				Assert.That(dialog.buttonOk.IsEffectivelyEnabled || dialog.buttonCancel.IsEffectivelyEnabled || dialog.buttonUploadLocalSolution.IsEffectivelyEnabled, Is.False);

				uploadProgress.Report(new RemoteOperationProgress("Uploading layer 'master', file 2 of 4", 42));
				await waitUntil(() => progressView.progressBar.Value == 42, "progress is shown");
				Assert.That(progressView.progressBar.IsIndeterminate, Is.False);
				Assert.That(progressView.labelOperationName.Text, Is.EqualTo("Uploading layer 'master', file 2 of 4"));
				Assert.That(progressView.Title, Is.EqualTo("Uploading solution 'props' (42%)"));

				click(progressView.buttonCancel);
				await waitUntil(() => !progressView.IsVisible && dialog.tabControl.IsEffectivelyEnabled, "upload is cancelled");
				Assert.That(uploadCancellation.IsCancellationRequested, Is.True);
				Assert.That(openedWindows.Where(w => w.IsVisible).OfType<MessageBox>(), Is.Empty, "nothing to report, no remote solution was created");
				Assert.That(dialog.buttonUploadLocalSolution.IsEnabled, Is.True);
				dialog.Close();
			}
			finally
			{
				SolutionHub.SetLocalStorageDefaultDirectory(originalDirectory);
			}
		}

		[AvaloniaTest]
		public async Task Remote_solution_deletion_shows_progress_and_cancelling_it_is_reported()
		{
			SolutionHub.RemoteStorageConnection = new TestStorageConnection { User = Alice };
			int enumerations = 0;
			SolutionBrowserDialog.EnumerateRemoteSolutionsAsync = (cancellationToken, logger) =>
			{
				enumerations++;
				return Task.FromResult<IEnumerable<ISolutionInfo>>(new[] { new TestSolutionInfo { Id = 7, ProjectName = "props", Location = $"https://account.blob.core.windows.net/fea-solutions/{Alice.Id}/7.solution.json" } });
			};
			IProgress<RemoteOperationProgress> deleteProgress = null;
			SolutionBrowserDialog.DeleteRemoteSolutionAsync = async (solutionId, progress, cancellationToken, logger) =>
			{
				Assert.That(solutionId, Is.EqualTo(7));
				deleteProgress = progress;
				await Task.Delay(Timeout.Infinite, cancellationToken); // until cancelled
			};

			var dialog = await showSolutionBrowserOnRemoteTab();
			dialog.remoteSolutionListView.listView.SelectedItem = dialog.remoteSolutionListView.listView.ItemsSource.Cast<object>().Single();
			click(dialog.buttonDeleteRemoteSolution);
			var confirmation = await waitForWindow<MessageBox>();
			click(findButton(confirmation, "ButtonYes"));

			var progressView = await waitForWindow<ProgressViewDialog>();
			Assert.That(progressView.Title, Is.EqualTo("Deleting remote solution 7"));
			Assert.That(dialog.tabControl.IsEffectivelyEnabled, Is.False, "the tabs can't be switched during the deletion");
			deleteProgress.Report(new RemoteOperationProgress("Deleting layer 'master', file 1 of 3", 25));
			await waitUntil(() => progressView.progressBar.Value == 25, "progress is shown");
			Assert.That(progressView.labelOperationName.Text, Is.EqualTo("Deleting layer 'master', file 1 of 3"));

			// cancelled: some layers can be deleted already, the solution is listed to be deleted again
			click(progressView.buttonCancel);
			var cancelledMessage = await waitForWindow<MessageBox>(w => w != confirmation);
			Assert.That(progressView.IsVisible, Is.False);
			string text = string.Join(" ", cancelledMessage.GetVisualDescendants().OfType<SelectableTextBlock>().Select(t => t.Text));
			Assert.That(text, Does.Contain("was cancelled").And.Contain("delete the solution again"));
			click(findButton(cancelledMessage, "ButtonOK"));
			await waitUntil(() => enumerations == 2, "remote solutions are reloaded");
			await dialog.RemoteLoadingTask;
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.tabControl.IsEffectivelyEnabled, Is.True);
			dialog.Close();
		}

		[Test]
		public async Task Uploaded_solution_is_owned_by_the_user_and_can_be_opened_modified_and_deleted()
		{
			var connection = await useTestStorageAccountAsync(Alice);
			string localSolutionFileName = importLocalSolution();
			var localDescription = SolutionHub.OpenLocal(localSolutionFileName).GetSolutionDescription();

			var remoteSolution = await SolutionHub.UploadLocalSolutionToRemoteAsync(localSolutionFileName, CancellationToken.None);
			try
			{
				Assert.That(remoteSolution.ProjectName, Is.EqualTo("props"));
				Assert.That(remoteSolution.Location, Does.EndWith($"/fea-solutions/{Alice.Id}/{remoteSolution.Id}.solution.json"));

				// owner is written to the solution, the layers are in the layer folder of the solution, in the folder of the user
				var serviceClient = new BlobServiceClient(connection.ConnectionString);
				string solutionJson = (await getSolutionBlob(serviceClient, Alice, remoteSolution.Id).DownloadContentAsync()).Value.Content.ToString();
				Assert.That(solutionJson, Does.Contain(Alice.Id).And.Contain(Alice.Name));
				string layerFolder = await getLayerFolderAsync(serviceClient, Alice, remoteSolution.Id);
				Assert.That(layerFolder, Does.StartWith($"{Alice.Id}/").And.Length.GreaterThan(Alice.Id.Length + 1));
				var layerBlobs = serviceClient.GetBlobContainerClient("fea-layers").GetBlobs(BlobTraits.None, BlobStates.None, $"{layerFolder}/{localDescription.Layers[0].Id}/", CancellationToken.None).ToList();
				Assert.That(layerBlobs, Is.Not.Empty);

				var remoteSolutions = await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None);
				Assert.That(remoteSolutions.Select(s => s.Id), Does.Contain(remoteSolution.Id));

				// open: same layers, data are read from the blob storage
				var remoteHub = SolutionHub.OpenRemote(remoteSolution.Id);
				var remoteDescription = await remoteHub.GetSolutionDescriptionAsync(CancellationToken.None);
				Assert.That(remoteDescription.Layers.Select(l => l.Id), Is.EqualTo(localDescription.Layers.Select(l => l.Id)));
				var masterLayerId = remoteDescription.Layers[0].Id;
				var summary = await remoteHub.LoadLayerSummaryAsync(masterLayerId, CancellationToken.None);
				Assert.That(summary.Fields, Is.Not.Empty);
				int meshIndex = summary.Meshes[0].Index;
				var geometry = await remoteHub.LoadGeometryAsync(masterLayerId, meshIndex, CancellationToken.None);
				Assert.That(geometry.NumberOfPoints, Is.GreaterThan(0));

				// new layer is written to the folder of the user and recorded in the remote solution, the owner stays
				var compressedLayer = await Task.Run(() => remoteHub.Compress(masterLayerId.ToString(), Enumerable.Empty<decimal>(), new[] { "Transparent" }, newLayerName: "compressed"));
				var reopenedDescription = await SolutionHub.OpenRemote(remoteSolution.Id).GetSolutionDescriptionAsync(CancellationToken.None);
				Assert.That(reopenedDescription.Layers[0].Children.Select(l => l.Id), Is.EqualTo(new[] { compressedLayer.Id }));
				var compressedGeometry = await SolutionHub.OpenRemote(remoteSolution.Id).LoadGeometryAsync(compressedLayer.Id, meshIndex, CancellationToken.None);
				Assert.That(compressedGeometry.NumberOfPoints, Is.EqualTo(geometry.NumberOfPoints));
				Assert.That(serviceClient.GetBlobContainerClient("fea-layers").GetBlobs(BlobTraits.None, BlobStates.None, $"{layerFolder}/{compressedLayer.Id}/", CancellationToken.None).Any(), Is.True);
				solutionJson = (await getSolutionBlob(serviceClient, Alice, remoteSolution.Id).DownloadContentAsync()).Value.Content.ToString();
				Assert.That(solutionJson, Does.Contain(Alice.Name), "owner is kept when the solution is modified");
				Assert.That(await getLayerFolderAsync(serviceClient, Alice, remoteSolution.Id), Is.EqualTo(layerFolder), "layer folder is kept when the solution is modified");

				await remoteHub.DeleteAsync(compressedLayer.Id.ToString(), deleteAll: false, CancellationToken.None);
				reopenedDescription = await SolutionHub.OpenRemote(remoteSolution.Id).GetSolutionDescriptionAsync(CancellationToken.None);
				Assert.That(reopenedDescription.Layers[0].Children, Is.Empty);
			}
			finally
			{
				connection.User = Alice;
				await SolutionHub.OpenRemote(remoteSolution.Id).DeleteAsync(layerIdOrName: null, deleteAll: true);
			}

			var remainingSolutions = await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None);
			Assert.That(remainingSolutions.Select(s => s.Id), Does.Not.Contain(remoteSolution.Id));
			Assert.ThrowsAsync<FileNotFoundException>(() => SolutionHub.OpenRemote(remoteSolution.Id).GetSolutionDescriptionAsync(CancellationToken.None));
		}

		[Test]
		public async Task Users_see_only_their_own_remote_solutions()
		{
			var connection = await useTestStorageAccountAsync(Alice);
			string localSolutionFileName = importLocalSolution();
			var aliceSolution = await SolutionHub.UploadLocalSolutionToRemoteAsync(localSolutionFileName, CancellationToken.None);
			connection.User = Bob;
			var bobSolution = await SolutionHub.UploadLocalSolutionToRemoteAsync(localSolutionFileName, CancellationToken.None);
			try
			{
				// stored in the folder of the user
				Assert.That(bobSolution.Location, Does.Contain($"/{Bob.Id}/"));

				connection.User = Bob;
				var bobsList = (await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None)).ToList();
				Assert.That(bobsList.Select(s => s.Location), Does.Contain(bobSolution.Location).And.Not.Contain(aliceSolution.Location));
				Assert.That(bobsList.Select(s => s.Location), Has.All.Contain($"/{Bob.Id}/"));

				connection.User = Alice;
				var alicesList = (await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None)).ToList();
				Assert.That(alicesList.Select(s => s.Location), Does.Contain(aliceSolution.Location).And.Not.Contain(bobSolution.Location));

				// a solution placed into the folder of another user is not listed nor opened
				var serviceClient = new BlobServiceClient(connection.ConnectionString);
				var solutions = serviceClient.GetBlobContainerClient("fea-solutions");
				var aliceBlob = solutions.GetBlobClient($"{Alice.Id}/{aliceSolution.Id}.solution.json");
				var foreignBlob = solutions.GetBlobClient($"{Bob.Id}/999.solution.json");
				await foreignBlob.UploadAsync((await aliceBlob.DownloadContentAsync()).Value.Content, overwrite: true);
				try
				{
					connection.User = Bob;
					Assert.That((await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None)).Select(s => s.Id), Does.Not.Contain(999));
					Assert.ThrowsAsync<FileNotFoundException>(() => SolutionHub.OpenRemote(999).GetSolutionDescriptionAsync(CancellationToken.None));
				}
				finally
				{
					await foreignBlob.DeleteIfExistsAsync();
				}

				// nobody signed in: no access
				connection.User = null;
				Assert.ThrowsAsync<RemoteSignInRequiredException>(() => SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None));
				Assert.Throws<RemoteSignInRequiredException>(() => SolutionHub.OpenRemote(aliceSolution.Id));
			}
			finally
			{
				connection.User = Alice;
				await SolutionHub.OpenRemote(aliceSolution.Id).DeleteAsync(layerIdOrName: null, deleteAll: true);
				connection.User = Bob;
				await SolutionHub.OpenRemote(bobSolution.Id).DeleteAsync(layerIdOrName: null, deleteAll: true);
			}
		}

		[Test]
		public async Task Remote_solutions_uploaded_from_the_same_local_solution_do_not_share_layers()
		{
			var connection = await useTestStorageAccountAsync(Alice);
			string localSolutionFileName = importLocalSolution();
			var first = await SolutionHub.UploadLocalSolutionToRemoteAsync(localSolutionFileName, CancellationToken.None);
			var second = await SolutionHub.UploadLocalSolutionToRemoteAsync(localSolutionFileName, CancellationToken.None);
			try
			{
				var serviceClient = new BlobServiceClient(connection.ConnectionString);
				Assert.That(await getLayerFolderAsync(serviceClient, Alice, first.Id), Is.Not.EqualTo(await getLayerFolderAsync(serviceClient, Alice, second.Id)));

				// the layers of the second one are kept when the first one is deleted
				await SolutionHub.DeleteRemoteSolutionAsync(first.Id, CancellationToken.None);
				var secondHub = SolutionHub.OpenRemote(second.Id);
				var masterLayerId = (await secondHub.GetSolutionDescriptionAsync(CancellationToken.None)).Layers[0].Id;
				var summary = await secondHub.LoadLayerSummaryAsync(masterLayerId, CancellationToken.None);
				var geometry = await secondHub.LoadGeometryAsync(masterLayerId, summary.Meshes[0].Index, CancellationToken.None);
				Assert.That(geometry.NumberOfPoints, Is.GreaterThan(0));
			}
			finally
			{
				foreach (var solution in new[] { first, second })
				{
					try
					{
						await SolutionHub.DeleteRemoteSolutionAsync(solution.Id, CancellationToken.None);
					}
					catch (FileNotFoundException)
					{ }
				}
			}
		}

		[Test]
		public async Task Remote_solution_with_layers_in_the_folder_of_the_user_is_opened_and_deleted()
		{
			// solutions uploaded before each solution got its own layer folder: no layer folder, layers directly in the folder of the user
			var connection = await useTestStorageAccountAsync(Alice);
			var remoteSolution = await SolutionHub.UploadLocalSolutionToRemoteAsync(importLocalSolution(), CancellationToken.None);
			var serviceClient = new BlobServiceClient(connection.ConnectionString);
			var layers = serviceClient.GetBlobContainerClient("fea-layers");
			string layerFolder = await getLayerFolderAsync(serviceClient, Alice, remoteSolution.Id);
			foreach (var blob in layers.GetBlobs(BlobTraits.None, BlobStates.None, layerFolder + "/", CancellationToken.None).ToList())
			{
				var content = (await layers.GetBlobClient(blob.Name).DownloadContentAsync()).Value.Content;
				await layers.GetBlobClient(Alice.Id + blob.Name.Substring(layerFolder.Length)).UploadAsync(content, overwrite: true);
				await layers.GetBlobClient(blob.Name).DeleteAsync();
			}
			var solutionBlob = getSolutionBlob(serviceClient, Alice, remoteSolution.Id);
			var solutionJson = JObject.Parse((await solutionBlob.DownloadContentAsync()).Value.Content.ToString());
			solutionJson.Remove("LayerFolder");
			await solutionBlob.UploadAsync(BinaryData.FromString(solutionJson.ToString()), overwrite: true);
			Assert.That(await getLayerFolderAsync(serviceClient, Alice, remoteSolution.Id), Is.EqualTo(Alice.Id));

			Guid masterLayerId;
			try
			{
				var hub = SolutionHub.OpenRemote(remoteSolution.Id);
				masterLayerId = (await hub.GetSolutionDescriptionAsync(CancellationToken.None)).Layers[0].Id;
				var summary = await hub.LoadLayerSummaryAsync(masterLayerId, CancellationToken.None);
				Assert.That(summary.Fields, Is.Not.Empty);
			}
			finally
			{
				await SolutionHub.DeleteRemoteSolutionAsync(remoteSolution.Id, CancellationToken.None);
			}
			Assert.That(layers.GetBlobs(BlobTraits.None, BlobStates.None, $"{Alice.Id}/{masterLayerId}/", CancellationToken.None).Any(), Is.False, "layers are deleted");
		}

		[Test]
		public async Task Cancelled_upload_deletes_the_uploaded_layer_files()
		{
			var connection = await useTestStorageAccountAsync(Alice);
			string localSolutionFileName = importLocalSolution();
			var layerIds = SolutionHub.OpenLocal(localSolutionFileName).GetSolutionDescription().Layers.Select(l => l.Id).ToList();
			int fileCount = layerIds.Sum(id => Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(localSolutionFileName), id.ToString()), "*", SearchOption.AllDirectories).Count());
			Assume.That(fileCount, Is.GreaterThanOrEqualTo(2), "a file is uploaded before the upload is cancelled");
			var solutionsBefore = (await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None)).Select(s => s.Id).ToList();

			// cancelled when the second file starts: the first one is uploaded
			var progress = new List<RemoteOperationProgress>();
			using var cancellationSource = new CancellationTokenSource();
			Assert.CatchAsync<OperationCanceledException>(() => SolutionHub.UploadLocalSolutionToRemoteAsync(localSolutionFileName, cancellationSource.Token, progress: new SynchronousProgress<RemoteOperationProgress>(p =>
			{
				progress.Add(p);
				if (p.Message.Contains("file 2 of "))
					cancellationSource.Cancel();
			})));

			Assert.That(progress.Select(p => p.Message), Has.Some.StartsWith("Deleting uploaded files, file 1 of "));
			var layerBlobNames = new BlobServiceClient(connection.ConnectionString).GetBlobContainerClient("fea-layers")
				.GetBlobs(BlobTraits.None, BlobStates.None, $"{Alice.Id}/", CancellationToken.None).Select(b => b.Name).ToList();
			Assert.That(layerBlobNames.Where(name => layerIds.Any(id => name.Contains($"/{id}/"))), Is.Empty, "uploaded files are deleted");
			Assert.That((await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None)).Select(s => s.Id), Is.EquivalentTo(solutionsBefore), "no solution is created");
		}

		[Test]
		public async Task Upload_and_deletion_report_progress_and_cancelled_deletion_can_be_finished()
		{
			await useTestStorageAccountAsync(Alice);
			string localSolutionFileName = importLocalSolution();
			string layerName = SolutionHub.OpenLocal(localSolutionFileName).GetSolutionDescription().Layers[0].Name;

			var uploadProgress = new List<RemoteOperationProgress>();
			var logger = new MemoryLogger();
			var remoteSolution = await SolutionHub.UploadLocalSolutionToRemoteAsync(localSolutionFileName, CancellationToken.None, logger, new SynchronousProgress<RemoteOperationProgress>(uploadProgress.Add));
			try
			{
				Assert.That(uploadProgress.First().Message, Does.StartWith($"Uploading layer '{layerName}', file 1 of "));
				Assert.That(uploadProgress.Last(), Is.EqualTo(new RemoteOperationProgress("Creating the remote solution", 100)));
				Assert.That(uploadProgress.Select(p => p.PercentDone), Is.Ordered, "the percentage never goes back");
				var loggedSteps = logger.GetRecordHistory().Where(r => r.Type == RecordType.OperationProgress).Select(r => r.Content);
				Assert.That(loggedSteps, Is.EqualTo(uploadProgress.Select(p => p.Message).Distinct()), "each step is logged once");

				// cancelled at the last step: all layers are deleted, but the solution is still listed
				var deleteProgress = new List<RemoteOperationProgress>();
				using var cancellationSource = new CancellationTokenSource();
				Assert.CatchAsync<OperationCanceledException>(() => SolutionHub.DeleteRemoteSolutionAsync(remoteSolution.Id, cancellationSource.Token, progress: new SynchronousProgress<RemoteOperationProgress>(p =>
				{
					deleteProgress.Add(p);
					if (p.Message == "Deleting the solution")
						cancellationSource.Cancel();
				})));
				Assert.That(deleteProgress.First(), Is.EqualTo(new RemoteOperationProgress("Listing layer files", -1)));
				Assert.That(deleteProgress[1].Message, Does.StartWith($"Deleting layer '{layerName}', file 1 of "));
				Assert.That(deleteProgress.Skip(1).Select(p => p.PercentDone), Is.Ordered.And.All.InRange(0, 99));
				Assert.That((await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None)).Select(s => s.Id), Does.Contain(remoteSolution.Id));

				// deleted again: the rest is deleted
				await SolutionHub.DeleteRemoteSolutionAsync(remoteSolution.Id, CancellationToken.None);
				Assert.That((await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None)).Select(s => s.Id), Does.Not.Contain(remoteSolution.Id));
			}
			finally
			{
				try
				{
					await SolutionHub.DeleteRemoteSolutionAsync(remoteSolution.Id, CancellationToken.None);
				}
				catch (FileNotFoundException)
				{ }
			}
		}

		[AvaloniaTest]
		public async Task Remote_solution_is_opened_in_postprocessor_from_solution_browser()
		{
			await useTestStorageAccountAsync(Alice);
			var remoteSolution = await SolutionHub.UploadLocalSolutionToRemoteAsync(importLocalSolution(), CancellationToken.None);
			try
			{
				var window = new MainWindow(Array.Empty<string>()) { WindowState = WindowState.Normal, Width = 1000, Height = 700 };
				window.Show();
				Dispatcher.UIThread.RunJobs();

				click(window.openSolutionToolStripMenuItem);
				var dialog = await waitForWindow<SolutionBrowserDialog>();
				Assert.That(dialog.tabPageRemoteSolutions.IsVisible, Is.True);
				dialog.tabControl.SelectedItem = dialog.tabPageRemoteSolutions;
				Dispatcher.UIThread.RunJobs();
				await dialog.RemoteLoadingTask;
				Dispatcher.UIThread.RunJobs();
				Assert.That(dialog.labelRemoteUser.Text, Is.EqualTo("Signed in as alice@example.com"), "the sign-in is remembered");

				var item = dialog.remoteSolutionListView.listView.ItemsSource.Cast<ISolutionInfo>().Single(s => s.Id == remoteSolution.Id);
				dialog.remoteSolutionListView.listView.SelectedItem = item;
				click(dialog.buttonOk);

				await waitUntil(() => window.LayoutMode == LayoutMode.Postprocessor, "solution is opened");
				var postprocessView = window.getCurrentPostprocessView();
				await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0 && window.ActiveViewport.SceneFacade.ContainsMesh, "layer is loaded");
				Assert.That(openedWindows.OfType<ExceptionReportDialog>(), Is.Empty);
				window.Close();
			}
			finally
			{
				await SolutionHub.OpenRemote(remoteSolution.Id).DeleteAsync(layerIdOrName: null, deleteAll: true);
			}
		}
	}
}
