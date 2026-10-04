using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Postprocessing;
using MeshEditor.CoreInterface;
using MeshEditor.LayerManager.Import;
using MeshEditor.SolutionManager;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Postprocessor: import of FEM results (GiD .msh + .res), solution browser, layers, data selection, filters.
	/// </summary>
	public class PostprocessorTests
	{
		private static string testDataPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

		private readonly List<Window> openedWindows = new List<Window>();
		private string tempDirectory;

		[SetUp]
		public void SetUp()
		{
			openedWindows.Clear();
			Window.WindowOpenedEvent.AddClassHandler<Window>((w, e) => openedWindows.Add(w), RoutingStrategies.Direct);
			tempDirectory = Path.Combine(Path.GetTempPath(), "MeshEditorTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDirectory);
			ErrorReports = () => openedWindows.OfType<ExceptionReportDialog>().Select(d => d.textBoxCaption.Text + ": " + d.textBoxExceptionMessage.Text + Environment.NewLine + d.textBoxStackTrace.Text).FirstOrDefault();
		}

		[TearDown]
		public void TearDown()
		{
			try
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
			catch (IOException)
			{ }
		}

		private static async Task waitUntil(Func<bool> condition, string description, int timeoutSeconds = 60)
		{
			var timeout = DateTime.UtcNow.AddSeconds(timeoutSeconds);
			while (true)
			{
				Dispatcher.UIThread.RunJobs();
				if (condition())
					break;
				if (DateTime.UtcNow > timeout)
					Assert.Fail($"timeout: {description}");
				if (ErrorReports?.Invoke() is string error)
					Assert.Fail($"{description}: error reported: {error}");
				await Task.Delay(20);
			}
			Dispatcher.UIThread.RunJobs();
		}

		/// <summary>
		/// Error reports shown during the test (the waiting fails immediately).
		/// </summary>
		private static Func<string> ErrorReports;

		private async Task<T> waitForWindow<T>() where T : Window
		{
			T window = null;
			await waitUntil(() => (window = openedWindows.OfType<T>().LastOrDefault(w => w.IsVisible)) != null, $"{typeof(T).Name} is opened");
			return window;
		}

		private static void click(Control control) => control.RaiseEvent(new RoutedEventArgs(control is MenuItem ? MenuItem.ClickEvent : Button.ClickEvent));

		private string importSolution()
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
			var solutionHub = SolutionHub.CreateNewLocal(Path.Combine(tempDirectory, "props"), analysisResults, "props");
			solutionHub.Import(Enumerable.Empty<decimal>(), Enumerable.Empty<string>(), "Nearest");
			return solutionHub.GetSolutionDescription().Location;
		}

		private static MainWindow showMainWindow()
		{
			var window = new MainWindow(Array.Empty<string>()) { WindowState = WindowState.Normal, Width = 1000, Height = 700 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return window;
		}

		private static async Task<PostprocessViewControl> openSolution(MainWindow window, string solutionFileName)
		{
			await window.openFiles(solutionFileName);
			var postprocessView = window.getCurrentPostprocessView();
			Assert.That(postprocessView, Is.Not.Null, "postprocessor layout");
			await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0 && window.ActiveViewport.SceneFacade.ContainsMesh, "layer is loaded");
			return postprocessView;
		}

		private static int countColoredPixels(byte[] bgra)
		{
			int count = 0;
			for (int i = 0; i < bgra.Length; i += 4)
			{
				int b = bgra[i], g = bgra[i + 1], r = bgra[i + 2];
				if (Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) > 60) // saturated colors of the color scale
					count++;
			}
			return count;
		}

		[AvaloniaTest]
		public async Task Import_FEM_results_dialog_creates_solution_and_opens_it()
		{
			var window = showMainWindow();
			click(window.importFEMResultsToolStripMenuItem);
			var dialog = await waitForWindow<ImportFEMResultsDialog>();

			Assert.That(dialog.Title, Is.EqualTo("Import FEM results"));
			Assert.That(dialog.tabControl.Items.Cast<TabItem>().Select(t => (string)t.Header), Is.EqualTo(new[] { "Result files", "Compression", "Gauss points extrapolation" }));
			Assert.That(dialog.buttonImport.IsEnabled, Is.False, "mesh file is required");

			dialog.textBoxMeshFile.Text = testDataPath("props.msh");
			dialog.textBoxResultFiles.Text = testDataPath("props.res");
			dialog.textBoxLocation.Text = tempDirectory;
			Dispatcher.UIThread.RunJobs(); // TextChanged
			Assert.That(dialog.textBoxProjectName.Text, Is.EqualTo("props"), "project name from the mesh file name");
			Assert.That(dialog.buttonImport.IsEnabled, Is.True);
			Assert.That(dialog.compressionParamsControl.GetCompressionParameters(), Is.Empty, "no compression by default");

			click(dialog.buttonImport);
			await waitUntil(() => !dialog.IsVisible, "import finishes and closes the dialog");

			Assert.That(dialog.SolutionFileName, Does.StartWith(Path.Combine(tempDirectory, "props")));
			Assert.That(File.Exists(dialog.SolutionFileName), Is.True);
			await waitUntil(() => window.LayoutMode == LayoutMode.Postprocessor, "postprocessor layout");
			var postprocessView = window.getCurrentPostprocessView();
			await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0, "layer is loaded");
			Assert.That(postprocessView.layersTreeView.RootNodes.Select(n => n.Name), Is.Not.Empty);
		}

		[AvaloniaTest]
		public void Compression_params_match_WinForms_logic()
		{
			var control = new CompressionParamsControl();
			Assert.That(control.comboBoxCompressionMethod.SelectedItem, Is.EqualTo("None"));
			Assert.That(control.groupBoxSVDCompressionParameters.IsEnabled, Is.False);
			Assert.That(control.textBoxNRMSD.Text, Is.EqualTo("1.0E-4"));
			Assert.That(control.GetKeyTimeSteps(), Is.Empty);

			control.comboBoxCompressionMethod.SelectedIndex = 1;
			Assert.That(control.groupBoxSVDCompressionParameters.IsEnabled, Is.True);
			Assert.That(control.checkBoxMergeTimeSteps.IsChecked, Is.True, "SVD merges time steps");
			Assert.That(control.GetCompressionParameters(), Is.EqualTo(new[] { "SVD", "error", control.textBoxNRMSD.Text }));
			Assert.That(control.GetKeyTimeSteps(), Is.EqualTo(new[] { decimal.MaxValue }));

			control.radioButtonSize.IsChecked = true;
			control.checkBoxSVDParameterRandomized.IsChecked = true;
			control.textBoxKeyTimeSteps.Text = "1 2.5";
			Assert.That(control.textBoxNRMSD.IsEnabled, Is.False);
			Assert.That(control.trackBarCompressionFactor.IsEnabled, Is.True);
			Assert.That(control.labelCompressionFactor.Text, Is.EqualTo("Compression factor: 10 %"));
			Assert.That(control.GetCompressionParameters(), Is.EqualTo(new[] { "SVD", "size", "0.1", "randomized" }));
			Assert.That(control.GetKeyTimeSteps(), Is.EqualTo(new[] { 1m, 2.5m }));
		}

		[AvaloniaTest]
		public async Task Opening_solution_shows_layers_data_selection_and_colored_mesh()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var dataSelection = postprocessView.dataSelectionControl;

			Assert.That(window.LayoutMode, Is.EqualTo(LayoutMode.Postprocessor));
			Assert.That(postprocessView.layersTreeView.RootNodes, Has.Count.EqualTo(1));
			Assert.That(postprocessView.layersTreeView.GetSelectedLayer(), Is.Not.Null, "first layer is selected");
			Assert.That(dataSelection.comboBoxTimeStep.ItemCount, Is.EqualTo(2));
			var fields = dataSelection.comboBoxField.Items.Select(i => i.ToString()).ToList();
			Assert.That(fields, Does.Contain("Displacement").And.Contain("Temperature"));

			window.ActiveViewport.Refresh();
			byte[] frame = window.ActiveViewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out _, out _);
			Assert.That(countColoredPixels(frame), Is.GreaterThan(1000), "mesh is colored by the selected field");

			// select the other field: the mesh is recolored
			var selection = dataSelection.CurrentDataSelection;
			dataSelection.comboBoxField.SelectedIndex = fields.IndexOf(selection.FieldName == "Temperature" ? "Displacement" : "Temperature");
			await waitUntil(() => dataSelection.CurrentDataSelection.FieldName != selection.FieldName, "field is changed");
			await waitUntil(() =>
			{
				window.ActiveViewport.Refresh();
				return !window.ActiveViewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out _, out _).SequenceEqual(frame);
			}, "mesh is recolored");

			// close solution: back to the preprocessor with an empty scene
			click(window.closeSolutionToolStripMenuItem);
			await waitUntil(() => window.LayoutMode == LayoutMode.Preprocessor, "preprocessor layout");
			Assert.That(window.getCurrentPostprocessView(), Is.Null);
			Assert.That(window.ActiveViewport.SceneFacade.ContainsMesh, Is.False);
		}

		[AvaloniaTest]
		public async Task Signal_data_minimum_and_maximum_return_IDs_of_mesh_nodes()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var dataSelection = postprocessView.dataSelectionControl;
			var sceneFacade = window.ActiveViewport.SceneFacade;
			int nodeCount = (int)sceneFacade.GetValue(AvailableValue.NodeCount);

			foreach (string field in new[] { "Temperature", "Displacement" })
			{
				dataSelection.comboBoxField.SelectedItem = dataSelection.comboBoxField.Items.Single(i => i.ToString() == field);
				IDataVisualizer dataVisualizer = null;
				await waitUntil(() => dataSelection.CurrentDataSelection.FieldName == field
					&& (dataVisualizer = sceneFacade.GetValue(AvailableValue.DataVisualizer) as IDataVisualizer) != null
					&& !double.IsNaN(dataVisualizer.GetMinimumDataValue()), $"{field} data is displayed");

				int[] minimumIDs = dataVisualizer.GetIDsOfNodesWithMinimumDataValue();
				int[] maximumIDs = dataVisualizer.GetIDsOfNodesWithMaximumDataValue();
				Assert.That(minimumIDs, Is.Not.Empty.And.All.InRange(1, nodeCount), $"{field} minimum");
				Assert.That(maximumIDs, Is.Not.Empty.And.All.InRange(1, nodeCount), $"{field} maximum");

				click(window.signalDataMinimumToolStripMenuItem);
				click(window.signalDataMaximumToolStripMenuItem);
				Dispatcher.UIThread.RunJobs();
				Assert.That(ErrorReports(), Is.Null);
			}
		}

		[AvaloniaTest]
		public async Task Visualizer_settings_dialogs_open_from_data_selection_and_apply_changes()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var dataSelection = postprocessView.dataSelectionControl;
			Assert.That(dataSelection.linkLabelEditColorScale.IsEnabled, Is.False, "no scalar field selected ([None])");

			dataSelection.comboBoxField.SelectedItem = dataSelection.comboBoxField.Items.Single(i => i.ToString() == "Temperature");
			await waitUntil(() => dataSelection.linkLabelEditColorScale.IsEnabled, "color scale settings are enabled");

			click(dataSelection.linkLabelEditColorScale);
			var colorScaleDialog = await waitForWindow<ColorScaleSettingsDialog>();
			Assert.That(colorScaleDialog.controlPointsPanel.Children, Is.Not.Empty, "control points of the color scale");
			window.ActiveViewport.Refresh();
			byte[] before = window.ActiveViewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out _, out _);
			colorScaleDialog.checkBoxShowIsoAreas.IsChecked = !colorScaleDialog.checkBoxShowIsoAreas.IsChecked;
			click(colorScaleDialog.buttonOK);
			await waitUntil(() => !colorScaleDialog.IsVisible, "dialog is closed");
			await waitUntil(() =>
			{
				window.ActiveViewport.Refresh();
				return !window.ActiveViewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out _, out _).SequenceEqual(before);
			}, "mesh colors are updated (iso areas)");

			Assert.That(dataSelection.linkLabelVectorFieldSettings.IsEnabled, Is.False);
			dataSelection.comboBoxVectorField.SelectedItem = dataSelection.comboBoxVectorField.Items.Single(i => i.ToString() == "Displacement");
			await waitUntil(() => dataSelection.linkLabelVectorFieldSettings.IsEnabled, "vector field settings are enabled");
			click(dataSelection.linkLabelVectorFieldSettings);
			var vectorDialog = await waitForWindow<VectorFieldVisualizationSettingsDialog>();
			click(vectorDialog.buttonCancel);
			await waitUntil(() => !vectorDialog.IsVisible, "dialog is closed");
		}

		[Test]
		public void Legend_text_has_proportional_character_widths()
		{
			var textPrinter = MeshEditor.Text.TextPrinter.Instance;
			float narrow = textPrinter.Measure("IIIIIIII").width;
			float wide = textPrinter.Measure("MMMMMMMM").width;
			Assert.That(narrow, Is.LessThan(wide / 2), "letters are not drawn in fixed-width cells");
			Assert.That(textPrinter.Measure("DISPLACEMENTS").height, Is.InRange(14f, 20f), "line height of the default font size");
			var lines = textPrinter.MeasureLines("DISPLACEMENTS" + Environment.NewLine + "X(1)");
			Assert.That(lines.width, Is.EqualTo(textPrinter.Measure("DISPLACEMENTS").width));
			Assert.That(lines.height, Is.GreaterThan(2 * textPrinter.Measure("X").height));
		}

		[AvaloniaTest]
		public async Task Color_scale_legend_is_drawn_with_text()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var dataSelection = postprocessView.dataSelectionControl;
			dataSelection.comboBoxField.SelectedItem = dataSelection.comboBoxField.Items.Single(i => i.ToString() == "Temperature");
			await waitUntil(() => dataSelection.linkLabelEditColorScale.IsEnabled, "scalar field is selected");

			byte[] frame = null;
			int width = 0, height = 0;
			string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "postprocessor-legend.png");
			try
			{
				// the legend (caption, color bar and values) is in the right top corner, text is drawn in the contrast color (black) on the light background
				await waitUntil(() =>
				{
					window.ActiveViewport.Refresh();
					frame = window.ActiveViewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out width, out height);
					return countDarkPixels(frame, width, height, x0: width - 250, y0: 0, x1: width, y1: Math.Min(height, 300)) > 200;
				}, "legend text is drawn", timeoutSeconds: 10);
			}
			finally
			{
				if (frame != null)
				{
					using var image = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Bgra32>(frame, width, height);
					SixLabors.ImageSharp.ImageExtensions.SaveAsPng(image, path);
					TestContext.Out.WriteLine(path);
				}
			}

			static int countDarkPixels(byte[] bgra, int width, int height, int x0, int y0, int x1, int y1)
			{
				int count = 0;
				for (int y = y0; y < y1; y++)
					for (int x = Math.Max(x0, 0); x < x1; x++)
					{
						int i = (y * width + x) * 4;
						if (bgra[i] < 100 && bgra[i + 1] < 100 && bgra[i + 2] < 100)
							count++;
					}
				return count;
			}
		}

		[AvaloniaTest]
		public async Task Deformation_filter_creates_child_layer()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var layersTree = postprocessView.layersTreeView;
			var rootNode = layersTree.RootNodes.Single();

			var contextMenu = layersTree.LayerContextMenu;
			Assert.That(contextMenu.Items.Select(i => i is Separator ? "-" : (string)((MenuItem)i).Header), Is.EqualTo(new[] { "Apply filter...", "Delete layer", "-", "Reload layer" }));
			var applyFilter = (MenuItem)contextMenu.Items[0];
			Assert.That(applyFilter.Items.Cast<MenuItem>().Select(i => (string)i.Header), Is.EqualTo(new[] { "Deformation...", "Slice...", "Iso-surface..." }));

			click((MenuItem)applyFilter.Items[0]);
			var dialog = await waitForWindow<DeformationFilterParamsDialog>();
			Assert.That(dialog.comboBoxDeformationField.SelectedItem, Is.EqualTo("Displacement"));
			Assert.That(dialog.textBoxLayerName.Text, Is.EqualTo("deformation (scale: 0.1)"));
			click(dialog.buttonOK);

			await waitUntil(() => rootNode.Children.Count == 1, "filter layer is added");
			var filterNode = rootNode.Children.Single();
			Assert.That(filterNode.Name, Is.EqualTo("deformation (scale: 0.1)"));
			Assert.That(rootNode.IsChecked, Is.True, "parent layer stays visible");
			await waitUntil(() => layersTree.GetSelectedLayer()?.Id == filterNode.Layer.Id, "filter layer is selected");
			await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0, "filter layer is loaded");

			// delete the filter layer
			click((MenuItem)contextMenu.Items[1]);
			await waitUntil(() => rootNode.Children.Count == 0, "filter layer is deleted");
			Assert.That(layersTree.GetSelectedLayer()?.Id, Is.EqualTo(rootNode.Layer.Id), "parent layer is selected");
		}

		[AvaloniaTest]
		public async Task Solution_browser_lists_local_solutions_and_returns_selection()
		{
			string solutionFileName = importSolution();
			string originalDirectory = SolutionHub.GetLocalStorageDefaultDirectory();
			SolutionHub.SetLocalStorageDefaultDirectory(tempDirectory);
			try
			{
				var window = showMainWindow();
				click(window.openSolutionToolStripMenuItem);
				var dialog = await waitForWindow<SolutionBrowserDialog>();
				await dialog.LoadingTask;
				Dispatcher.UIThread.RunJobs();

				Assert.That(dialog.Title, Is.EqualTo("Open solution"));
				Assert.That(dialog.buttonOk.IsEnabled, Is.False, "nothing selected");
				var items = dialog.localSolutionListView.listView.ItemsSource.Cast<object>().ToList();
				Assert.That(items, Has.Count.EqualTo(1));

				dialog.localSolutionListView.listView.SelectedItem = items[0];
				Assert.That(dialog.buttonOk.IsEnabled && dialog.buttonDeleteLocalSolution.IsEnabled, Is.True);
				Assert.That(Path.GetFullPath(dialog.LocalSolutionFileName), Is.EqualTo(Path.GetFullPath(solutionFileName)));

				click(dialog.buttonOk);
				await waitUntil(() => window.LayoutMode == LayoutMode.Postprocessor, "solution is opened");
				var postprocessView = window.getCurrentPostprocessView();
				await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0, "layer is loaded");
			}
			finally
			{
				SolutionHub.SetLocalStorageDefaultDirectory(originalDirectory);
			}
		}
	}
}
