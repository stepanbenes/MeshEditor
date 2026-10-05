using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.Common.Extensions;
using MeshEditor.Common.Logging;
using MeshEditor.CoreInterface;
using MeshEditor.LayerManager.Import;
using MeshEditor.SolutionManager;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// Compression parameters of the imported results (port of MeshEditor.DataVisualizer.UI.CompressionParamsControl).
	/// </summary>
	public class CompressionParamsControl : StackPanel
	{
		internal readonly ComboBox comboBoxCompressionMethod = new ComboBox { Width = 160, ItemsSource = new[] { "None", "SVD" } };
		internal readonly HeaderedContentControl groupBoxSVDCompressionParameters;
		internal readonly RadioButton radioButtonError = new RadioButton { Content = "Compression error", GroupName = "svdPreference" };
		internal readonly RadioButton radioButtonSize = new RadioButton { Content = "Compressed size", GroupName = "svdPreference" };
		internal readonly TextBlock labelNRMSD = new TextBlock { Text = "NRMSD", VerticalAlignment = VerticalAlignment.Center };
		internal readonly TextBox textBoxNRMSD = new TextBox { Width = 127 };
		internal readonly TextBlock labelCompressionFactor = new TextBlock { Text = "Compression factor" };
		internal readonly Slider trackBarCompressionFactor = new Slider { Minimum = 0, Maximum = 100, TickFrequency = 1, IsSnapToTickEnabled = true, TickPlacement = TickPlacement.BottomRight };
		internal readonly CheckBox checkBoxSVDParameterRandomized = new CheckBox { Content = "Use Randomized SVD to accelerate compression algorithm" };
		internal readonly CheckBox checkBoxMergeTimeSteps = new CheckBox { Content = "Merge time steps" };
		internal readonly TextBox textBoxKeyTimeSteps = new TextBox();

		public CompressionParamsControl()
		{
			Spacing = 6;
			var methodRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = "Compression method", VerticalAlignment = VerticalAlignment.Center }, comboBoxCompressionMethod } };
			var nrmsdRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(22, 0, 0, 0), Children = { labelNRMSD, textBoxNRMSD } };
			labelCompressionFactor.Margin = new Thickness(22, 0, 0, 0);
			trackBarCompressionFactor.Margin = new Thickness(22, 0, 0, 0);
			groupBoxSVDCompressionParameters = new HeaderedContentControl
			{
				Header = "SVD parameters",
				Content = new StackPanel
				{
					Spacing = 4,
					Children = { new TextBlock { Text = "Preference" }, radioButtonError, nrmsdRow, radioButtonSize, labelCompressionFactor, trackBarCompressionFactor, checkBoxSVDParameterRandomized },
				},
			};
			if (Application.Current?.TryFindResource("GroupBox", out object theme) == true && theme is ControlTheme groupBoxTheme)
				groupBoxSVDCompressionParameters.Theme = groupBoxTheme;

			Children.Add(methodRow);
			Children.Add(groupBoxSVDCompressionParameters);
			Children.Add(checkBoxMergeTimeSteps);
			Children.Add(new TextBlock { Text = "Key time steps" });
			Children.Add(textBoxKeyTimeSteps);

			comboBoxCompressionMethod.SelectionChanged += (s, e) =>
			{
				checkBoxMergeTimeSteps.IsChecked = comboBoxCompressionMethod.SelectedIndex > 0;
				updateUI();
			};
			checkBoxMergeTimeSteps.IsCheckedChanged += (s, e) => updateUI();
			radioButtonError.IsCheckedChanged += (s, e) => updateUI();
			radioButtonSize.IsCheckedChanged += (s, e) => updateUI();
			trackBarCompressionFactor.ValueChanged += (s, e) =>
			{
				labelCompressionFactor.Text = $"Compression factor: {(int)trackBarCompressionFactor.Value} %";
				updateUI();
			};

			comboBoxCompressionMethod.SelectedIndex = 0;
			radioButtonError.IsChecked = true;
			textBoxNRMSD.Text = 1.0E-4.ToString("0.0E0", CultureInfo.InvariantCulture);
			trackBarCompressionFactor.Value = 10;
			updateUI();
		}

		private void updateUI()
		{
			groupBoxSVDCompressionParameters.IsEnabled = comboBoxCompressionMethod.SelectedIndex > 0;
			textBoxKeyTimeSteps.IsEnabled = checkBoxMergeTimeSteps.IsChecked == true;
			labelCompressionFactor.IsEnabled = trackBarCompressionFactor.IsEnabled = radioButtonSize.IsChecked == true;
			labelNRMSD.IsEnabled = textBoxNRMSD.IsEnabled = radioButtonError.IsChecked == true;
		}

		public IEnumerable<string> GetCompressionParameters()
		{
			List<string> parameters = new List<string>();
			if (comboBoxCompressionMethod.SelectedIndex > 0)
			{
				parameters.Add((string)comboBoxCompressionMethod.SelectedItem);

				// add compression factor parameters
				if (radioButtonError.IsChecked == true)
				{
					parameters.Add("error");
					parameters.Add(textBoxNRMSD.Text);
				}
				else if (radioButtonSize.IsChecked == true)
				{
					parameters.Add("size");
					parameters.Add(((int)trackBarCompressionFactor.Value * 0.01).ToString(CultureInfo.InvariantCulture)); // parsed invariantly (the WinForms app runs with the English culture)
				}
				if (checkBoxSVDParameterRandomized.IsChecked == true)
				{
					parameters.Add("randomized");
				}
			}
			return parameters;
		}

		public IEnumerable<decimal> GetKeyTimeSteps()
		{
			if (checkBoxMergeTimeSteps.IsChecked == true)
			{
				string[] tokens = (textBoxKeyTimeSteps.Text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (!tokens.Any())
					return Enumerable.Repeat(decimal.MaxValue, 1); // NOTE: was double.PositiveInfinity, but decimal does not have infinity
				return tokens.Select(token => decimal.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture)); // TODO: handle parsing errors better
			}
			else
			{
				return Enumerable.Empty<decimal>();
			}
		}
	}

	/// <summary>
	/// Import of FEM analysis results into a new local solution (port of MeshEditor.DataVisualizer.UI.ImportFEMResultsForm).
	/// </summary>
	public class ImportFEMResultsDialog : Window
	{
		private bool isImportOperationRunning;
		private readonly LongOpNotifier longOpNotifier;
		private CancellationTokenSource currentOperationCancellationTokenSource;
		private bool dialogResult;

		internal readonly TabControl tabControl;
		internal readonly TextBox textBoxMeshFile = new TextBox();
		internal readonly TextBox textBoxResultFiles = new TextBox();
		internal readonly TextBox textBoxProjectName = new TextBox { Width = 175, HorizontalAlignment = HorizontalAlignment.Left };
		internal readonly TextBox textBoxLocation = new TextBox();
		internal readonly CheckBox checkBoxCreateDirectoryForSolution = new CheckBox { Content = "Create directory for solution", IsChecked = true };
		internal readonly CompressionParamsControl compressionParamsControl = new CompressionParamsControl();
		internal readonly ComboBox comboBoxGaussPointExtrapolationStrategy = new ComboBox { Width = 160, ItemsSource = new[] { "Nearest" } };
		internal readonly Button buttonImport = new Button { Content = "Import", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = true };
		internal readonly Button buttonClose = new Button { Content = "Close", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center, IsCancel = true };

		public ImportFEMResultsDialog(LongOpNotifier longOpNotifier)
		{
			Title = "Import FEM results";
			Width = 598;
			SizeToContent = SizeToContent.Height;
			CanResize = false;
			CanMinimize = CanMaximize = false;
			ShowInTaskbar = false;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			var buttonChooseMeshFile = new Button { Content = "...", Width = 57, HorizontalContentAlignment = HorizontalAlignment.Center };
			var buttonChooseResultFiles = new Button { Content = "...", Width = 57, HorizontalContentAlignment = HorizontalAlignment.Center };
			var buttonChooseSolutionDirectory = new Button { Content = "...", Width = 57, HorizontalContentAlignment = HorizontalAlignment.Center };
			Control fileRow(TextBox textBox, Button button)
			{
				var row = new DockPanel();
				DockPanel.SetDock(button, Dock.Right);
				button.Margin = new Thickness(8, 0, 0, 0);
				row.Children.Add(button);
				row.Children.Add(textBox);
				return row;
			}
			TextBlock label(string text, double top = 8) => new TextBlock { Text = text, Margin = new Thickness(0, top, 0, 2) };

			var resultFilesPage = new StackPanel
			{
				Children =
				{
					label("Mesh file", 4), fileRow(textBoxMeshFile, buttonChooseMeshFile),
					label("Result files"), fileRow(textBoxResultFiles, buttonChooseResultFiles),
					label("Project name", 40), textBoxProjectName,
					label("Location"), fileRow(textBoxLocation, buttonChooseSolutionDirectory),
					checkBoxCreateDirectoryForSolution,
				},
			};
			var gaussPage = new StackPanel { Margin = new Thickness(0, 4, 0, 0), Spacing = 2, Children = { new TextBlock { Text = "Strategy" }, comboBoxGaussPointExtrapolationStrategy } };
			tabControl = new TabControl
			{
				Margin = new Thickness(12, 12, 12, 0),
				Padding = new Thickness(0, 6, 0, 0),
				Items =
				{
					new TabItem { Header = "Result files", Content = resultFilesPage },
					new TabItem { Header = "Compression", Content = compressionParamsControl },
					new TabItem { Header = "Gauss points extrapolation", Content = gaussPage },
				},
			};
			var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12, 8, 12, 12), Children = { buttonImport, buttonClose } };
			var root = new DockPanel();
			DockPanel.SetDock(buttons, Dock.Bottom);
			root.Children.Add(buttons);
			root.Children.Add(tabControl);
			Content = root;

			this.longOpNotifier = longOpNotifier;
			this.longOpNotifier.CancellationRequested += longOpNotifier_CancellationRequested;

			buttonImport.Click += buttonImport_Click;
			buttonClose.Click += (s, e) => close(false);
			buttonChooseMeshFile.Click += buttonChooseMeshFile_Click;
			buttonChooseResultFiles.Click += buttonChooseResultFiles_Click;
			buttonChooseSolutionDirectory.Click += buttonChooseSolutionDirectory_Click;
			textBoxMeshFile.TextChanged += (s, e) => textBoxProjectName.Text = Path.GetFileNameWithoutExtension((textBoxMeshFile.Text ?? "").RemoveQuotes()).MakeAlphanumeric();
			textBoxProjectName.TextChanged += (s, e) => updateUI();
			textBoxLocation.TextChanged += (s, e) => updateUI();

			comboBoxGaussPointExtrapolationStrategy.SelectedIndex = 0;
			textBoxLocation.Text = SolutionHub.GetLocalStorageDefaultDirectory();

			// update state of UI
			updateUI();
		}

		public string SolutionFileName { get; private set; }

		/// <summary>
		/// Shows the dialog, returns true if the results were imported (DialogResult.OK).
		/// </summary>
		public new async Task<bool> ShowDialog(Window owner)
		{
			await base.ShowDialog(owner);
			return dialogResult;
		}

		private void close(bool result)
		{
			dialogResult = result;
			Close();
		}

		protected override void OnClosing(WindowClosingEventArgs e)
		{
			if (isImportOperationRunning) // the WinForms form is disabled while importing (cancel via status bar)
				e.Cancel = true;
			base.OnClosing(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			longOpNotifier.CancellationRequested -= longOpNotifier_CancellationRequested;
			base.OnClosed(e);
		}

		private void longOpNotifier_CancellationRequested(LongOpNotifier.Token obj)
		{
			if (currentOperationCancellationTokenSource != null)
			{
				currentOperationCancellationTokenSource.Cancel();
				currentOperationCancellationTokenSource.Dispose();
				currentOperationCancellationTokenSource = null;
			}
		}

		private async void buttonImport_Click(object sender, RoutedEventArgs e)
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(textBoxMeshFile.Text));

			var logger = new MemoryLogger();
			SolutionHub solutionHub = null;
			bool imported = false;
			try
			{
				currentOperationCancellationTokenSource = new CancellationTokenSource();
				var cancellationToken = currentOperationCancellationTokenSource.Token;

				isImportOperationRunning = true;
				updateUI();

				string[] resultFiles = (textBoxResultFiles.Text ?? "").SplitToTokensWithQuotes();

				IEnumerable<AnalysisResult> analysisResults = new[]
					{
						new AnalysisResult
						{
							TimeStep = null, // TODO: use explicit time steps for .vtu
							MeshRecordNames = new[] { textBoxMeshFile.Text.RemoveQuotes() },
							DataRecordNames = resultFiles
						}
					};
				string projectName = textBoxProjectName.Text;
				string location = textBoxLocation.Text;
				string solutionDirectory;
				if (checkBoxCreateDirectoryForSolution.IsChecked == true)
				{
					solutionDirectory = Path.Combine(location, projectName.MakeAlphanumeric());
				}
				else
				{
					solutionDirectory = location;
				}

				var keyTimeSteps = compressionParamsControl.GetKeyTimeSteps();
				var compressionParameters = compressionParamsControl.GetCompressionParameters();
				var gaussPointsExtrapolationStrategyName = buildGaussPointsExtrapolationStrategyName();

				using (longOpNotifier.Begin("Importing FEM results", isCancellable: true, logger: logger))
				{
					solutionHub = SolutionHub.CreateNewLocal(solutionDirectory, analysisResults, projectName, logger);
					await Task.Run(() => solutionHub.Import(keyTimeSteps, compressionParameters, gaussPointsExtrapolationStrategyName, cancellationToken: cancellationToken));
					SolutionFileName = solutionHub.GetSolutionDescription().Location;
				}
				imported = true;
			}
			catch (OperationCanceledException)
			{
				// user cancelled import operation, remove solution
				if (solutionHub != null)
				{
					await solutionHub.DeleteAsync(layerIdOrName: null, deleteAll: true);
				}
			}
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show("Import FEM results", ex, logger);
			}
			finally
			{
				isImportOperationRunning = false;
				updateUI();
			}
			if (imported)
				close(true);
		}

		private string buildGaussPointsExtrapolationStrategyName()
		{
			return comboBoxGaussPointExtrapolationStrategy.SelectedItem as string;
		}

		private async void buttonChooseMeshFile_Click(object sender, RoutedEventArgs e)
		{
			var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Open",
				AllowMultiple = false,
				FileTypeFilter = FileDialogFilter.Parse(SceneFacade.ImportMeshFileFormatFilter),
			});
			string path = files.FirstOrDefault()?.TryGetLocalPath();
			if (path != null)
				textBoxMeshFile.Text = path.QuoteIfContainsWhiteSpace();
		}

		private async void buttonChooseResultFiles_Click(object sender, RoutedEventArgs e)
		{
			var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Open",
				AllowMultiple = true,
				FileTypeFilter = FileDialogFilter.Parse(SceneFacade.ImportDataFileFormatFilter),
			});
			string[] paths = files.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToArray();
			if (paths.Length > 0)
				textBoxResultFiles.Text = string.Join(" ", paths.Select(filename => filename.QuoteIfContainsWhiteSpace()));
		}

		private async void buttonChooseSolutionDirectory_Click(object sender, RoutedEventArgs e)
		{
			var options = new FolderPickerOpenOptions { AllowMultiple = false };
			string currentDirectory = SolutionHub.GetLocalStorageDefaultDirectory();
			if (!string.IsNullOrEmpty(currentDirectory) && Directory.Exists(currentDirectory))
				options.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(currentDirectory);
			var folders = await StorageProvider.OpenFolderPickerAsync(options);
			string path = folders.FirstOrDefault()?.TryGetLocalPath();
			if (path != null)
				textBoxLocation.Text = path;
		}

		private void updateUI()
		{
			buttonImport.IsEnabled = !isImportOperationRunning && !string.IsNullOrWhiteSpace(textBoxMeshFile.Text) && !string.IsNullOrWhiteSpace(textBoxProjectName.Text) && !string.IsNullOrWhiteSpace(textBoxLocation.Text);
			tabControl.IsEnabled = !isImportOperationRunning;
			buttonClose.IsEnabled = !isImportOperationRunning;
		}
	}
}
