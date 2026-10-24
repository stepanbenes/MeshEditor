using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MeshEditor.AvaloniaUI.Postprocessing;
using MeshEditor.CoreInterface;
using MeshEditor.DataVisualizer;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager.Import;
using MeshEditor.SolutionManager;
using NUnit.Framework;
using MouseButton = Avalonia.Input.MouseButton;

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

		private string importSolution(string solutionName = "props", params string[] compressionParameters)
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
			solutionHub.Import(Enumerable.Empty<decimal>(), compressionParameters, "Nearest");
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
		public async Task Import_FEM_results_dialog_has_a_default_location_so_a_mesh_file_enables_import()
		{
			// without a configured directory the default is the documents folder, or the home folder when there is
			// none (e.g. WSL without ~/Documents, where the empty location kept the Import button disabled)
			string originalDirectory = SolutionHub.GetLocalStorageDefaultDirectory();
			SolutionHub.SetLocalStorageDefaultDirectory(null);
			try
			{
				string defaultDirectory = SolutionHub.GetLocalStorageDefaultDirectory();
				Assert.That(defaultDirectory, Is.Not.Empty);
				Assert.That(Directory.Exists(defaultDirectory), Is.True, defaultDirectory);

				var window = showMainWindow();
				click(window.importFEMResultsToolStripMenuItem);
				var dialog = await waitForWindow<ImportFEMResultsDialog>();
				Assert.That(dialog.textBoxLocation.Text, Is.EqualTo(defaultDirectory));

				dialog.textBoxMeshFile.Text = testDataPath("props.msh");
				Dispatcher.UIThread.RunJobs(); // TextChanged
				Assert.That(dialog.buttonImport.IsEnabled, Is.True, "mesh file is the only required input");
				dialog.Close();
			}
			finally
			{
				SolutionHub.SetLocalStorageDefaultDirectory(originalDirectory);
			}
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
		public async Task Import_with_invalid_key_time_step_is_reported_before_creating_the_solution()
		{
			var window = showMainWindow();
			click(window.importFEMResultsToolStripMenuItem);
			var dialog = await waitForWindow<ImportFEMResultsDialog>();
			dialog.textBoxMeshFile.Text = testDataPath("props.msh");
			dialog.textBoxResultFiles.Text = testDataPath("props.res");
			dialog.textBoxLocation.Text = tempDirectory;
			dialog.compressionParamsControl.comboBoxCompressionMethod.SelectedIndex = 1; // SVD merges time steps
			dialog.compressionParamsControl.textBoxKeyTimeSteps.Text = "1 2,5";
			Dispatcher.UIThread.RunJobs();

			click(dialog.buttonImport);
			var errorBox = await waitForWindow<Dialogs.MessageBox>();
			Assert.That(errorBox.Title, Is.EqualTo("Invalid compression parameter"));
			Assert.That(errorBox.GetVisualDescendants().OfType<SelectableTextBlock>().Single().Text, Does.Contain("'2,5'"));
			Assert.That(dialog.tabControl.SelectedIndex, Is.EqualTo(1), "the Compression tab is shown");

			errorBox.Close();
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.IsVisible, Is.True, "the dialog stays open for correction");
			Assert.That(Directory.Exists(tempDirectory) ? Directory.GetFileSystemEntries(tempDirectory) : Array.Empty<string>(), Is.Empty, "no solution is created");
		}

		[AvaloniaTest]
		public void Compression_params_are_validated()
		{
			var control = new CompressionParamsControl();
			Assert.That(control.ValidateParameters(), Is.Null);
			control.textBoxNRMSD.Text = "abc";
			Assert.That(control.ValidateParameters(), Is.Null, "NRMSD is not used without compression");

			control.comboBoxCompressionMethod.SelectedIndex = 1;
			Assert.That(control.ValidateParameters(), Does.Contain("NRMSD 'abc'"));
			control.textBoxNRMSD.Text = "-1";
			Assert.That(control.ValidateParameters(), Does.Contain("NRMSD '-1'"));
			control.textBoxNRMSD.Text = "2.5E-3";
			Assert.That(control.ValidateParameters(), Is.Null);

			control.textBoxKeyTimeSteps.Text = "0.5 x 2";
			Assert.That(control.ValidateParameters(), Does.Contain("key time step 'x'"));
			control.textBoxKeyTimeSteps.Text = "0.5  1 2.5";
			Assert.That(control.ValidateParameters(), Is.Null);
			Assert.That(control.GetKeyTimeSteps(), Is.EqualTo(new[] { 0.5m, 1m, 2.5m }));
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
		public async Task Results_imported_with_SVD_compression_match_uncompressed_results()
		{
			// SVD used a native library (redsvd) that was not shipped with the .NET build (issue #11)
			async Task<(double Minimum, double Maximum)> temperatureRange(string solutionFileName)
			{
				var window = showMainWindow();
				var postprocessView = await openSolution(window, solutionFileName);
				var dataSelection = postprocessView.dataSelectionControl;
				dataSelection.comboBoxField.SelectedItem = dataSelection.comboBoxField.Items.Single(i => i.ToString() == "Temperature");
				IDataVisualizer dataVisualizer = null;
				await waitUntil(() => dataSelection.CurrentDataSelection.FieldName == "Temperature"
					&& (dataVisualizer = window.ActiveViewport.SceneFacade.GetValue(AvailableValue.DataVisualizer) as IDataVisualizer) != null
					&& !double.IsNaN(dataVisualizer.GetMinimumDataValue()), "Temperature data is displayed");
				window.ActiveViewport.Refresh();
				byte[] frame = window.ActiveViewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out _, out _);
				Assert.That(countColoredPixels(frame), Is.GreaterThan(1000), "mesh is colored by the selected field");
				var range = (dataVisualizer.GetMinimumDataValue(), dataVisualizer.GetMaximumDataValue());
				window.Close();
				return range;
			}

			var (minimum, maximum) = await temperatureRange(importSolution("uncompressed"));
			Assert.That(maximum, Is.GreaterThan(minimum));
			double tolerance = 1e-9 * (maximum - minimum);

			var exact = await temperatureRange(importSolution("svd", "SVD"));
			Assert.That(exact.Minimum, Is.EqualTo(minimum).Within(tolerance), "full-rank SVD is lossless");
			Assert.That(exact.Maximum, Is.EqualTo(maximum).Within(tolerance), "full-rank SVD is lossless");

			foreach (var parameters in new[] { new[] { "SVD", "error", "0.01" }, new[] { "SVD", "error", "0.01", "randomized" }, new[] { "SVD", "size", "0.5", "randomized" } })
			{
				var compressed = await temperatureRange(importSolution(string.Join("-", parameters), parameters));
				Assert.That(compressed.Minimum, Is.Not.NaN, string.Join(" ", parameters));
				Assert.That(compressed.Maximum, Is.Not.NaN, string.Join(" ", parameters));
			}
			Assert.That(ErrorReports(), Is.Null);
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

		[AvaloniaTest]
		public void Color_scale_dialog_keeps_data_range_loaded_while_it_is_open([Values] bool ok)
		{
			// the scalar data (and so the min/max of the color scale) is loaded asynchronously, the dialog can be opened before
			var settings = new VisualizerSettings();
			settings.ColorScale.SetMinMaxValue(0.0, 1.0);
			var dialog = new ColorScaleSettingsDialog(settings);
			dialog.Show();
			dialog.checkBoxShowIsoAreas.IsChecked = !settings.DrawIsoAreas;

			settings.ColorScale.SetMinMaxValue(20.0, 80.0); // data loaded
			click(ok ? dialog.buttonOK : dialog.buttonCancel);

			Assert.That((settings.ColorScale.MinValue, settings.ColorScale.MaxValue), Is.EqualTo((20.0, 80.0)));
			Assert.That(settings.ColorScale.ControlPoints.First().Value, Is.EqualTo(20.0));
			Assert.That(settings.ColorScale.ControlPoints.Last().Value, Is.EqualTo(80.0));
			Assert.That(settings.ColorScale.GetColorForValue(50.0), Is.Not.EqualTo(ColorScale.OutOfRangeColor));
			Assert.That(settings.DrawIsoAreas, Is.EqualTo(ok), "the other settings are applied (OK) or restored (Cancel)");
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
					MeshEditor.AvaloniaUI.Rendering.ScreenshotFile.Save(path, frame, width, height);
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
		public async Task Slice_filter_plane_is_defined_by_picking_nodes_and_creates_child_layer()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var rootNode = postprocessView.layersTreeView.RootNodes.Single();
			var applyFilter = (MenuItem)postprocessView.layersTreeView.LayerContextMenu.Items[0];

			click((MenuItem)applyFilter.Items[1]);
			var dialog = await waitForWindow<SliceFilterParamsDialog>();
			Assert.That(dialog.buttonOK.IsEnabled, Is.False, "no plane yet");
			Assert.That(postprocessView.layersTreeView.GetSelectedLayer()?.Id, Is.EqualTo(rootNode.Layer.Id), "the points are picked on the selected layer");
			click(dialog.buttonSelectTwoPoints);
			Assert.That(SceneFacade.EditorMode, Is.EqualTo(EditorMode.PickCuttingPlanePoint));
			Assert.That(dialog.labelSuggestion.IsVisible, Is.True);

			// props.msh is a 2x2 grid of quads seen from the front: its nodes are at the corners and in the centre of the white
			// square of the faces; the plane through the left top corner and the centre (and the view direction) cuts it diagonally
			var viewport = window.ActiveViewport;
			var scene = viewport.SceneFacade;
			viewport.Refresh();
			byte[] frame = viewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out int width, out int height);
			int left = width, top = height, right = 0, bottom = 0;
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					int i = (y * width + x) * 4;
					if (frame[i] > 245 && frame[i + 1] > 245 && frame[i + 2] > 245)
					{
						left = Math.Min(left, x); right = Math.Max(right, x);
						top = Math.Min(top, y); bottom = Math.Max(bottom, y);
					}
				}
			Assert.That(right - left, Is.GreaterThan(100), "the mesh faces are drawn white");
			double pixelsPerUnit = width / viewport.Bounds.Width;
			foreach (var (x, y) in new[] { (left, top), ((left + right) / 2, (top + bottom) / 2) })
			{
				var point = viewport.TranslatePoint(new Point(x / pixelsPerUnit, y / pixelsPerUnit), window) ?? throw new InvalidOperationException("viewport is not in the window");
				window.MouseMove(point);
				window.MouseDown(point, MouseButton.Left);
				window.MouseUp(point, MouseButton.Left);
				Dispatcher.UIThread.RunJobs();
			}
			Assert.That(scene.CutPlanes, Has.Count.EqualTo(1), "the plane is created from the two picked nodes (which are cleared then)");
			Assert.That(dialog.buttonOK.IsEnabled, Is.True);
			Assert.That(dialog.labelSuggestion.IsVisible, Is.False);
			Assert.That(dialog.textBoxLayerName.Text, Does.Match(@"^slice -?\d+\.\d\d$"), "layer name with the plane offset");
			string layerName = dialog.textBoxLayerName.Text;

			click(dialog.buttonOK);
			await waitUntil(() => rootNode.Children.Count == 1, "slice layer is added");
			Assert.That(rootNode.Children.Single().Name, Is.EqualTo(layerName));
			Assert.That(SceneFacade.EditorMode, Is.EqualTo(EditorMode.Orbit), "picking ends with the dialog");
			await waitUntil(() => postprocessView.layersTreeView.GetSelectedLayer()?.Id == rootNode.Children.Single().Layer.Id, "slice layer is selected");
			await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0, "slice layer is loaded");
			Assert.That(ErrorReports(), Is.Null);
		}

		[AvaloniaTest]
		public async Task Iso_surface_filter_creates_child_layer()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var rootNode = postprocessView.layersTreeView.RootNodes.Single();

			// an iso-value in the middle of the temperature range
			var dataSelection = postprocessView.dataSelectionControl;
			dataSelection.comboBoxField.SelectedItem = dataSelection.comboBoxField.Items.Single(i => i.ToString() == "Temperature");
			IDataVisualizer dataVisualizer = null;
			await waitUntil(() => dataSelection.CurrentDataSelection.FieldName == "Temperature"
				&& (dataVisualizer = window.ActiveViewport.SceneFacade.GetValue(AvailableValue.DataVisualizer) as IDataVisualizer) != null
				&& !double.IsNaN(dataVisualizer.GetMinimumDataValue()), "Temperature data is displayed");
			double isoValue = (dataVisualizer.GetMinimumDataValue() + dataVisualizer.GetMaximumDataValue()) / 2;

			var applyFilter = (MenuItem)postprocessView.layersTreeView.LayerContextMenu.Items[0];
			click((MenuItem)applyFilter.Items[2]);
			var dialog = await waitForWindow<IsoSurfaceFilterParamsDialog>();
			dialog.comboBoxField.SelectedItem = "Temperature";
			Assert.That(dialog.comboBoxComponent.ItemCount, Is.GreaterThan(0));
			dialog.textBoxValue.Text = isoValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
			Dispatcher.UIThread.RunJobs(); // TextChanged
			string layerName = dialog.textBoxLayerName.Text;
			Assert.That(layerName, Does.StartWith("isosurface Temperature/").And.EndWith(" = " + isoValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));

			click(dialog.buttonOK);
			await waitUntil(() => rootNode.Children.Count == 1, "iso-surface layer is added");
			Assert.That(rootNode.Children.Single().Name, Is.EqualTo(layerName));
			await waitUntil(() => postprocessView.layersTreeView.GetSelectedLayer()?.Id == rootNode.Children.Single().Layer.Id, "iso-surface layer is selected");
			await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0, "iso-surface layer is loaded");
			Assert.That(ErrorReports(), Is.Null);
		}

		/// <summary>
		/// Real mouse click (pointer pressed + released) on the control (near its left edge: long layer names are clipped by the panel).
		/// </summary>
		private static void mouseClick(Window window, Control control, MouseButton button = MouseButton.Left)
		{
			var point = control.TranslatePoint(new Point(Math.Min(control.Bounds.Width / 2, 8), control.Bounds.Height / 2), window) ?? throw new InvalidOperationException("control is not in the window");
			window.MouseMove(point);
			window.MouseDown(point, button);
			window.MouseUp(point, button);
			Dispatcher.UIThread.RunJobs();
		}

		private static TreeViewItem treeViewItemOf(LayersTreeViewControl layersTree, LayerTreeNode node) =>
			layersTree.TreeView.GetVisualDescendants().OfType<TreeViewItem>().Single(i => i.DataContext == node);

		private static TextBlock layerNameLabelOf(LayersTreeViewControl layersTree, LayerTreeNode node) =>
			treeViewItemOf(layersTree, node).GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == node.Name);

		private static Control layerCheckBoxOf(LayersTreeViewControl layersTree, LayerTreeNode node) =>
			treeViewItemOf(layersTree, node).GetVisualDescendants().OfType<CheckBox>().First().GetVisualDescendants().OfType<Border>().First(b => b.Name == "NormalRectangle");

		[AvaloniaTest]
		public async Task Left_click_on_layer_selects_it_without_context_menu()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var layersTree = postprocessView.layersTreeView;
			var rootNode = layersTree.RootNodes.Single();

			// add a child layer (it gets selected)
			click((MenuItem)((MenuItem)layersTree.LayerContextMenu.Items[0]).Items[0]);
			var dialog = await waitForWindow<DeformationFilterParamsDialog>();
			click(dialog.buttonOK);
			await waitUntil(() => rootNode.Children.Count == 1, "filter layer is added");
			var filterNode = rootNode.Children.Single();
			await waitUntil(() => layersTree.GetSelectedLayer()?.Id == filterNode.Layer.Id, "filter layer is selected");
			await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0, "filter layer is loaded");
			Dispatcher.UIThread.RunJobs();

			// left click on the name of the root layer selects it, doesn't open the context menu and doesn't toggle the check box
			bool rootChecked = rootNode.IsChecked;
			mouseClick(window, layerNameLabelOf(layersTree, rootNode));
			Assert.That(layersTree.GetSelectedLayer()?.Id, Is.EqualTo(rootNode.Layer.Id), "root layer is selected by left click");
			Assert.That(layersTree.LayerContextMenu.IsOpen, Is.False, "context menu is not opened by left click");
			Assert.That(rootNode.IsChecked, Is.EqualTo(rootChecked), "clicking the name doesn't toggle visibility");
			await waitUntil(() => postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0, "root layer is loaded");

			// check box of a non-selected layer is toggled without changing the selection
			bool filterChecked = filterNode.IsChecked;
			mouseClick(window, layerCheckBoxOf(layersTree, filterNode));
			Assert.That(filterNode.IsChecked, Is.EqualTo(!filterChecked), "non-selected layer is toggled");
			Assert.That(layersTree.GetSelectedLayer()?.Id, Is.EqualTo(rootNode.Layer.Id), "selection is not changed by the check box");

			// check box of the selected layer is toggled too
			mouseClick(window, layerNameLabelOf(layersTree, filterNode));
			Assert.That(layersTree.GetSelectedLayer()?.Id, Is.EqualTo(filterNode.Layer.Id), "filter layer is selected by left click");
			mouseClick(window, layerCheckBoxOf(layersTree, filterNode));
			Assert.That(filterNode.IsChecked, Is.EqualTo(filterChecked), "selected layer is toggled");

			// right click on the selected layer opens the context menu
			mouseClick(window, layerNameLabelOf(layersTree, filterNode), MouseButton.Right);
			Assert.That(layersTree.LayerContextMenu.IsOpen, Is.True, "context menu is opened by right click");
			layersTree.LayerContextMenu.Close();
		}

		[AvaloniaTest]
		public async Task Switching_layers_keeps_data_selection_of_each_layer()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var layersTree = postprocessView.layersTreeView;
			var dataSelection = postprocessView.dataSelectionControl;
			var rootNode = layersTree.RootNodes.Single();
			LayerDataVisualizer currentDataVisualizer() => postprocessView.ActiveScene.GetValue(AvailableValue.DataVisualizer) as LayerDataVisualizer;

			// add a child layer (it gets selected) and uncheck it (its mesh is unloaded when another layer is selected)
			click((MenuItem)((MenuItem)layersTree.LayerContextMenu.Items[0]).Items[0]);
			var dialog = await waitForWindow<DeformationFilterParamsDialog>();
			click(dialog.buttonOK);
			await waitUntil(() => rootNode.Children.Count == 1, "filter layer is added");
			var filterNode = rootNode.Children.Single();
			await waitUntil(() => layersTree.GetSelectedLayer()?.Id == filterNode.Layer.Id && dataSelection.comboBoxField.ItemCount > 0 && currentDataVisualizer() != null, "filter layer is loaded");
			mouseClick(window, layerCheckBoxOf(layersTree, filterNode));
			Assert.That(filterNode.IsChecked, Is.False);

			// select the second time step and a scalar field of the filter layer
			Assert.That(dataSelection.comboBoxTimeStep.ItemCount, Is.EqualTo(2));
			dataSelection.comboBoxTimeStep.SelectedIndex = 1;
			dataSelection.comboBoxField.SelectedItem = dataSelection.comboBoxField.Items.Single(i => i.ToString() == "Temperature");
			var selection = dataSelection.CurrentDataSelection;
			await waitUntil(() => currentDataVisualizer()?.DataSelection is DataSelection s && s.TimeStep == selection.TimeStep && s.FieldName == "Temperature" && s.ComponentName == selection.ComponentName, "filter layer shows the selected data");
			var filterDataVisualizer = currentDataVisualizer();
			var settings = filterDataVisualizer.Settings;

			// select the parent layer (checked, stays loaded) and then the filter layer again (its mesh is unloaded meanwhile because it is unchecked)
			layersTree.SetSelectedLayer(rootNode.Layer.Id);
			await waitUntil(() => currentDataVisualizer() is LayerDataVisualizer v && v != filterDataVisualizer && dataSelection.comboBoxField.ItemCount > 0, "parent layer is selected");
			await Task.Delay(200);
			Dispatcher.UIThread.RunJobs();
			layersTree.SetSelectedLayer(filterNode.Layer.Id);
			await waitUntil(() => currentDataVisualizer() is LayerDataVisualizer v && v.DataSelection?.Mesh != null && dataSelection.CurrentDataSelection != null, "filter layer is loaded again");
			await Task.Delay(200);
			Dispatcher.UIThread.RunJobs();

			var restoredSelection = dataSelection.CurrentDataSelection;
			Assert.That(restoredSelection.TimeStep, Is.EqualTo(selection.TimeStep), "time step is kept");
			Assert.That(restoredSelection.FieldName, Is.EqualTo("Temperature"), "field is kept");
			Assert.That(restoredSelection.ComponentName, Is.EqualTo(selection.ComponentName), "component is kept");
			Assert.That(currentDataVisualizer().DataSelection.TimeStep, Is.EqualTo(selection.TimeStep), "mesh shows the kept time step");
			Assert.That(currentDataVisualizer().DataSelection.FieldName, Is.EqualTo("Temperature"), "mesh shows the kept field");
			Assert.That(currentDataVisualizer().Settings, Is.SameAs(settings), "visualizer settings (color scale) are kept");
			Assert.That(dataSelection.linkLabelEditColorScale.IsEnabled, Is.True);
		}

		/// <summary>
		/// Number of white pixels (faces of a mesh without scalar data; the background is gray).
		/// </summary>
		private static int countMeshPixels(MainWindow window)
		{
			window.ActiveViewport.Refresh();
			byte[] bgra = window.ActiveViewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out _, out _);
			int count = 0;
			for (int i = 0; i < bgra.Length; i += 4)
			{
				if (bgra[i] > 245 && bgra[i + 1] > 245 && bgra[i + 2] > 245)
					count++;
			}
			return count;
		}

		[AvaloniaTest]
		public async Task Layer_visibility_follows_check_boxes_and_selected_layer_is_always_shown()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var layersTree = postprocessView.layersTreeView;
			var rootNode = layersTree.RootNodes.Single();
			var postprocessScene = (PostprocessScene)postprocessView.ActiveScene.GetUnderlyingSceneObject();
			Dispatcher.UIThread.RunJobs();

			// the first layer is selected and shown
			Assert.That(rootNode.IsChecked, Is.True, "first layer is checked");
			Assert.That(countMeshPixels(window), Is.GreaterThan(1000), "first layer is shown");

			// the selected layer stays shown when it is unchecked
			mouseClick(window, layerCheckBoxOf(layersTree, rootNode));
			Assert.That(rootNode.IsChecked, Is.False);
			Assert.That(layersTree.GetSelectedLayer()?.Id, Is.EqualTo(rootNode.Layer.Id), "layer stays selected");
			Assert.That(countMeshPixels(window), Is.GreaterThan(1000), "unchecked selected layer is shown");
			mouseClick(window, layerCheckBoxOf(layersTree, rootNode));
			Assert.That(countMeshPixels(window), Is.GreaterThan(1000), "checked selected layer is shown");

			// a new filter layer is selected and shown, the parent layer is switched to border lines
			click((MenuItem)((MenuItem)layersTree.LayerContextMenu.Items[0]).Items[0]);
			var dialog = await waitForWindow<DeformationFilterParamsDialog>();
			click(dialog.buttonOK);
			await waitUntil(() => rootNode.Children.Count == 1, "filter layer is added");
			var filterNode = rootNode.Children.Single();
			await waitUntil(() => layersTree.GetSelectedLayer()?.Id == filterNode.Layer.Id && postprocessScene.IsLayerLoaded(filterNode.Layer.Id) && postprocessView.dataSelectionControl.comboBoxField.ItemCount > 0, "filter layer is loaded");
			Dispatcher.UIThread.RunJobs();
			Assert.That(filterNode.IsChecked, Is.True, "new filter layer is checked");

			// unchecking both layers keeps the selected filter layer shown (the parent layer is not selected)
			mouseClick(window, layerCheckBoxOf(layersTree, rootNode));
			mouseClick(window, layerCheckBoxOf(layersTree, filterNode));
			Assert.That(rootNode.IsChecked || filterNode.IsChecked, Is.False);
			Assert.That(layersTree.GetSelectedLayer()?.Id, Is.EqualTo(filterNode.Layer.Id), "selection is not changed by the check boxes");
			Assert.That(countMeshPixels(window), Is.GreaterThan(1000), "unchecked selected layer is shown");

			// select the unchecked parent layer: the mesh of the unchecked filter layer is unloaded, only the border lines of the parent are drawn
			mouseClick(window, layerNameLabelOf(layersTree, rootNode));
			await waitUntil(() => postprocessScene.IsLayerLoaded(rootNode.Layer.Id) && !postprocessScene.IsLayerLoaded(filterNode.Layer.Id), "parent layer is selected");
			await Task.Delay(200);
			Dispatcher.UIThread.RunJobs();
			Assert.That(postprocessView.ActiveScene.GetValue(AvailableValue.RenderMode), Is.EqualTo(MeshEditor.Graphics.RenderMode.BorderLines));
			Assert.That(countMeshPixels(window), Is.LessThan(100), "no faces are drawn");
			postprocessView.ActiveScene.SetValue(AvailableValue.RenderMode, MeshEditor.Graphics.RenderMode.FacesLines);
			Assert.That(countMeshPixels(window), Is.GreaterThan(1000), "unchecked selected layer is shown");
			postprocessView.ActiveScene.SetValue(AvailableValue.RenderMode, MeshEditor.Graphics.RenderMode.BorderLines);

			// checking the non-selected (unloaded) filter layer loads and shows it, the parent layer stays selected
			mouseClick(window, layerCheckBoxOf(layersTree, filterNode));
			await waitUntil(() => postprocessScene.IsLayerLoaded(filterNode.Layer.Id), "filter layer is loaded");
			await Task.Delay(200);
			Dispatcher.UIThread.RunJobs();
			Assert.That(countMeshPixels(window), Is.GreaterThan(1000), "checked non-selected layer is shown");
			Assert.That(layersTree.GetSelectedLayer()?.Id, Is.EqualTo(rootNode.Layer.Id), "parent layer stays selected");
			Assert.That(postprocessView.ActiveScene.GetValue(AvailableValue.SelectedLayerId), Is.EqualTo(rootNode.Layer.Id));

			// unchecking the non-selected filter layer hides it, its mesh stays loaded
			mouseClick(window, layerCheckBoxOf(layersTree, filterNode));
			Assert.That(postprocessScene.IsLayerLoaded(filterNode.Layer.Id), Is.True);
			Assert.That(countMeshPixels(window), Is.LessThan(100), "unchecked non-selected layer is hidden");
		}

		[AvaloniaTest]
		public async Task Checking_layers_quickly_loads_all_of_them()
		{
			string solutionFileName = importSolution();
			var window = showMainWindow();
			var postprocessView = await openSolution(window, solutionFileName);
			var layersTree = postprocessView.layersTreeView;
			var rootNode = layersTree.RootNodes.Single();
			var postprocessScene = (PostprocessScene)postprocessView.ActiveScene.GetUnderlyingSceneObject();

			// two filter layers
			for (int i = 1; i <= 2; i++)
			{
				layersTree.SetSelectedLayer(rootNode.Layer.Id);
				await waitUntil(() => postprocessScene.IsLayerLoaded(rootNode.Layer.Id), "parent layer is loaded");
				Dispatcher.UIThread.RunJobs();
				click((MenuItem)((MenuItem)layersTree.LayerContextMenu.Items[0]).Items[0]);
				var dialog = await waitForWindow<DeformationFilterParamsDialog>();
				dialog.textBoxLayerName.Text = $"filter {i}";
				click(dialog.buttonOK);
				await waitUntil(() => rootNode.Children.Count == i && postprocessScene.IsLayerLoaded(rootNode.Children[i - 1].Layer.Id), $"filter layer {i} is loaded");
				Dispatcher.UIThread.RunJobs();
			}
			var filterNodes = rootNode.Children.ToList();

			// uncheck both and select the parent layer: their meshes are unloaded
			foreach (var node in filterNodes)
				mouseClick(window, layerCheckBoxOf(layersTree, node));
			mouseClick(window, layerNameLabelOf(layersTree, rootNode));
			await waitUntil(() => filterNodes.All(n => !postprocessScene.IsLayerLoaded(n.Layer.Id)), "filter layers are unloaded");
			await Task.Delay(200);
			Dispatcher.UIThread.RunJobs();

			// check both right after each other: the load of the second one cancels the load of the first one, which must be retried
			// (not by mouse clicks: headless input runs the dispatcher jobs, so the first load would finish before the second click)
			foreach (var node in filterNodes)
				layersTree.SetCheckedFlagOfLayer(node.Layer.Id, true);
			await waitUntil(() => filterNodes.All(n => postprocessScene.IsLayerLoaded(n.Layer.Id)), "both checked layers are loaded");
			Assert.That(filterNodes.All(n => n.IsChecked), Is.True);
			Assert.That(layersTree.GetSelectedLayer()?.Id, Is.EqualTo(rootNode.Layer.Id), "parent layer stays selected");
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
