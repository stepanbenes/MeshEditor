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
using MeshEditor.AvaloniaUI.Postprocessing;
using MeshEditor.Common.Logging;
using MeshEditor.LayerManager.Import;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.AzureStorage;
using MeshEditor.SolutionManager.IO;
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
		}

		[TearDown]
		public void TearDown()
		{
			SolutionHub.RemoteStorageConnection = originalConnection;
			SolutionBrowserDialog.EnumerateRemoteSolutionsAsync = originalEnumerateRemoteSolutions;
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

				// owner is written to the solution, the layers are in the folder of the user
				var serviceClient = new BlobServiceClient(connection.ConnectionString);
				string solutionJson = (await serviceClient.GetBlobContainerClient("fea-solutions").GetBlobClient($"{Alice.Id}/{remoteSolution.Id}.solution.json").DownloadContentAsync()).Value.Content.ToString();
				Assert.That(solutionJson, Does.Contain(Alice.Id).And.Contain(Alice.Name));
				var layerBlobs = serviceClient.GetBlobContainerClient("fea-layers").GetBlobs(BlobTraits.None, BlobStates.None, $"{Alice.Id}/{localDescription.Layers[0].Id}/", CancellationToken.None).ToList();
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
				Assert.That(serviceClient.GetBlobContainerClient("fea-layers").GetBlobs(BlobTraits.None, BlobStates.None, $"{Alice.Id}/{compressedLayer.Id}/", CancellationToken.None).Any(), Is.True);
				solutionJson = (await serviceClient.GetBlobContainerClient("fea-solutions").GetBlobClient($"{Alice.Id}/{remoteSolution.Id}.solution.json").DownloadContentAsync()).Value.Content.ToString();
				Assert.That(solutionJson, Does.Contain(Alice.Name), "owner is kept when the solution is modified");

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
