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
	/// Remote solutions in Azure Blob Storage. The tests that need a storage account run against the Azurite emulator
	/// (e.g. "npx azurite-blob --inMemoryPersistence --skipApiVersionCheck") or against the account given by the environment
	/// variable MESHEDITOR_TEST_AZURE_STORAGE_CONNECTION_STRING; they are ignored when neither is available.
	/// </summary>
	public class RemoteSolutionsTests
	{
		private const string TestConnectionStringEnvironmentVariable = "MESHEDITOR_TEST_AZURE_STORAGE_CONNECTION_STRING";
		private const string AzuriteConnectionString = "UseDevelopmentStorage=true";

		private static string testDataPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

		private readonly List<Window> openedWindows = new List<Window>();
		private string tempDirectory;
		private string originalConnectionStringVariable;
		private string originalConfigurationDirectory;
		private Func<bool> originalIsRemoteStorageConfigured;
		private Func<CancellationToken, MemoryLogger, Task<IEnumerable<ISolutionInfo>>> originalEnumerateRemoteSolutions;

		[SetUp]
		public void SetUp()
		{
			openedWindows.Clear();
			Window.WindowOpenedEvent.AddClassHandler<Window>((w, e) => openedWindows.Add(w), RoutingStrategies.Direct);
			tempDirectory = Path.Combine(Path.GetTempPath(), "MeshEditorTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDirectory);
			originalConnectionStringVariable = Environment.GetEnvironmentVariable(AzureStorageCredentialStore.ConnectionStringEnvironmentVariable);
			// never read or write the secure storage of the real user configuration
			originalConfigurationDirectory = MeshEditor.Common.ConfigurationManager.ConfigurationDirectory;
			MeshEditor.Common.ConfigurationManager.ConfigurationDirectory = Path.Combine(tempDirectory, "config");
			originalIsRemoteStorageConfigured = SolutionBrowserDialog.IsRemoteStorageConfigured;
			originalEnumerateRemoteSolutions = SolutionBrowserDialog.EnumerateRemoteSolutionsAsync;
		}

		[TearDown]
		public void TearDown()
		{
			Environment.SetEnvironmentVariable(AzureStorageCredentialStore.ConnectionStringEnvironmentVariable, originalConnectionStringVariable);
			MeshEditor.Common.ConfigurationManager.ConfigurationDirectory = originalConfigurationDirectory;
			SolutionBrowserDialog.IsRemoteStorageConfigured = originalIsRemoteStorageConfigured;
			SolutionBrowserDialog.EnumerateRemoteSolutionsAsync = originalEnumerateRemoteSolutions;
			foreach (var window in openedWindows.Where(w => w.IsVisible).ToList())
				window.Close();
			try
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
			catch (IOException)
			{ }
		}

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

		/// <summary>
		/// Points the remote storage to the test account (environment variable), ignores the test if there is none.
		/// </summary>
		private static void useTestStorageAccount()
		{
			string connectionString = Environment.GetEnvironmentVariable(TestConnectionStringEnvironmentVariable);
			if (string.IsNullOrWhiteSpace(connectionString))
			{
				if (!isAzuriteRunning())
					Assert.Ignore($"No test storage: start Azurite or set {TestConnectionStringEnvironmentVariable}.");
				connectionString = AzuriteConnectionString;
			}
			Environment.SetEnvironmentVariable(AzureStorageCredentialStore.ConnectionStringEnvironmentVariable, connectionString);
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

		private async Task<T> waitForWindow<T>() where T : Window
		{
			T window = null;
			await waitUntil(() => (window = openedWindows.OfType<T>().LastOrDefault(w => w.IsVisible)) != null, $"{typeof(T).Name} is opened");
			return window;
		}

		private static void click(Control control) => control.RaiseEvent(new RoutedEventArgs(control is MenuItem ? MenuItem.ClickEvent : Button.ClickEvent));

		private class TestSolutionInfo : ISolutionInfo
		{
			public int Id { get; set; }
			public string ProjectName { get; set; }
			public string Location { get; set; }
		}

		[Test]
		public void Credential_store_keeps_connection_string_outside_the_configuration_file()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("On Linux and macOS the store uses the keyring of the user, which is shared with the real application.");
			Environment.SetEnvironmentVariable(AzureStorageCredentialStore.ConnectionStringEnvironmentVariable, null);
			const string secret = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=c2VjcmV0;EndpointSuffix=core.windows.net";
			try
			{
				AzureStorageCredentialStore.SetConnectionString(secret);
				Assert.That(AzureStorageCredentialStore.GetConnectionString(), Is.EqualTo(secret));
				Assert.That(SolutionHub.IsRemoteStorageConfigured, Is.True);

				// the stored file is encrypted, it doesn't contain the key in plain text
				foreach (string file in Directory.EnumerateFiles(MeshEditor.Common.ConfigurationManager.ConfigurationDirectory))
					Assert.That(File.ReadAllText(file), Does.Not.Contain("c2VjcmV0"), file);

				// environment variable takes precedence
				Environment.SetEnvironmentVariable(AzureStorageCredentialStore.ConnectionStringEnvironmentVariable, "UseDevelopmentStorage=true");
				Assert.That(AzureStorageCredentialStore.GetConnectionString(), Is.EqualTo("UseDevelopmentStorage=true"));
				Assert.That(AzureStorageCredentialStore.IsProvidedByEnvironment, Is.True);
				Environment.SetEnvironmentVariable(AzureStorageCredentialStore.ConnectionStringEnvironmentVariable, null);
			}
			finally
			{
				AzureStorageCredentialStore.Clear();
			}
			Assert.That(AzureStorageCredentialStore.GetConnectionString(), Is.Null);
			Assert.That(SolutionHub.IsRemoteStorageConfigured, Is.False);
		}

		[AvaloniaTest]
		public async Task Remote_solutions_tab_is_hidden_when_remote_storage_is_not_configured()
		{
			SolutionBrowserDialog.IsRemoteStorageConfigured = () => false;
			var dialog = new SolutionBrowserDialog();
			dialog.Show();
			await dialog.LoadingTask;
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.tabPageRemoteSolutions.IsVisible, Is.False);
			Assert.That(dialog.buttonUploadLocalSolution.IsVisible, Is.False);
			Assert.That(dialog.buttonRemoteStorage.IsVisible, Is.True, "the remote storage can be configured");
			Assert.That(dialog.RemoteLoadingTask, Is.Null, "remote solutions are not loaded");
			dialog.Close();
		}

		[AvaloniaTest]
		public async Task Remote_solutions_tab_lists_remote_solutions_and_returns_selection()
		{
			SolutionBrowserDialog.IsRemoteStorageConfigured = () => true;
			SolutionBrowserDialog.EnumerateRemoteSolutionsAsync = (cancellationToken, logger) => Task.FromResult<IEnumerable<ISolutionInfo>>(new[]
			{
				new TestSolutionInfo { Id = 7, ProjectName = "props", Location = "https://account.blob.core.windows.net/fea-solutions/7.solution.json" },
				new TestSolutionInfo { Id = 8, ProjectName = "props", Location = "https://account.blob.core.windows.net/fea-solutions/8.solution.json" },
			});
			var dialog = new SolutionBrowserDialog();
			dialog.Show();
			await dialog.LoadingTask;
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.tabPageRemoteSolutions.IsVisible, Is.True);
			Assert.That(dialog.buttonUploadLocalSolution.IsVisible, Is.True);
			Assert.That(dialog.RemoteLoadingTask, Is.Null, "remote solutions are loaded when the tab is opened");

			dialog.tabControl.SelectedItem = dialog.tabPageRemoteSolutions;
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.RemoteLoadingTask, Is.Not.Null);
			await dialog.RemoteLoadingTask;
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.buttonOk.IsEnabled, Is.False, "nothing selected");
			var items = dialog.remoteSolutionListView.listView.ItemsSource.Cast<ISolutionInfo>().ToList();
			Assert.That(items.Select(s => s.Id), Is.EqualTo(new[] { 7, 8 }));

			dialog.remoteSolutionListView.listView.SelectedItem = items[1];
			Assert.That(dialog.buttonOk.IsEnabled && dialog.buttonDeleteRemoteSolution.IsEnabled, Is.True);
			Assert.That(dialog.SolutionLocation, Is.EqualTo(SolutionBrowserDialog.SolutionLocationType.Remote));
			Assert.That(dialog.RemoteSolutionId, Is.EqualTo(8));

			// switching back to the local tab drops the remote selection
			dialog.tabControl.SelectedItem = dialog.tabPageLocalSolutions;
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.SolutionLocation, Is.Not.EqualTo(SolutionBrowserDialog.SolutionLocationType.Remote));
			Assert.That(dialog.RemoteSolutionId, Is.Null);
			dialog.Close();
		}

		[Test]
		public async Task Uploaded_solution_can_be_listed_opened_modified_and_deleted()
		{
			useTestStorageAccount();
			string localSolutionFileName = importLocalSolution();
			var localDescription = SolutionHub.OpenLocal(localSolutionFileName).GetSolutionDescription();

			var remoteSolution = await SolutionHub.UploadLocalSolutionToRemoteAsync(localSolutionFileName, CancellationToken.None);
			try
			{
				Assert.That(remoteSolution.ProjectName, Is.EqualTo("props"));
				Assert.That(remoteSolution.Location, Does.EndWith($"/{remoteSolution.Id}.solution.json"));

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

				// new layer is written to the blob storage and recorded in the remote solution
				var compressedLayer = await Task.Run(() => remoteHub.Compress(masterLayerId.ToString(), Enumerable.Empty<decimal>(), new[] { "Transparent" }, newLayerName: "compressed"));
				var reopenedDescription = await SolutionHub.OpenRemote(remoteSolution.Id).GetSolutionDescriptionAsync(CancellationToken.None);
				Assert.That(reopenedDescription.Layers[0].Children.Select(l => l.Id), Is.EqualTo(new[] { compressedLayer.Id }));
				var compressedGeometry = await SolutionHub.OpenRemote(remoteSolution.Id).LoadGeometryAsync(compressedLayer.Id, meshIndex, CancellationToken.None);
				Assert.That(compressedGeometry.NumberOfPoints, Is.EqualTo(geometry.NumberOfPoints));

				await remoteHub.DeleteAsync(compressedLayer.Id.ToString(), deleteAll: false, CancellationToken.None);
				reopenedDescription = await SolutionHub.OpenRemote(remoteSolution.Id).GetSolutionDescriptionAsync(CancellationToken.None);
				Assert.That(reopenedDescription.Layers[0].Children, Is.Empty);
			}
			finally
			{
				await SolutionHub.OpenRemote(remoteSolution.Id).DeleteAsync(layerIdOrName: null, deleteAll: true);
			}

			var remainingSolutions = await SolutionHub.EnumerateAllRemoteSolutionsAsync(CancellationToken.None);
			Assert.That(remainingSolutions.Select(s => s.Id), Does.Not.Contain(remoteSolution.Id));
			Assert.ThrowsAsync<FileNotFoundException>(() => SolutionHub.OpenRemote(remoteSolution.Id).GetSolutionDescriptionAsync(CancellationToken.None));
		}

		[AvaloniaTest]
		public async Task Remote_solution_is_opened_in_postprocessor_from_solution_browser()
		{
			useTestStorageAccount();
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
