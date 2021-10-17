using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager;
using MeshEditor.Common.Logging;
using MeshEditor.Common.Extensions;
using System.IO;
using MeshEditor.LayerManager.Import;
using MeshEditor.CoreInterface;
using System.Threading;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class ImportFEMResultsForm : Form
	{
		bool isImportOperationRunning;
		readonly LongOpNotifier longOpNotifier;
		CancellationTokenSource currentOperationCancellationTokenSource;

		public ImportFEMResultsForm(LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.longOpNotifier = longOpNotifier;
			this.longOpNotifier.CancellationRequested += longOpNotifier_CancellationRequested;

			comboBoxGaussPointExtrapolationStrategy.SelectedIndex = 0;
			textBoxLocation.Text = SolutionHub.GetLocalStorageDefaultDirectory();

			// update state of UI
			updateUI();
		}

		public string SolutionFileName { get; private set; }

		protected override void OnClosed(EventArgs e)
		{
			longOpNotifier.CancellationRequested -= longOpNotifier_CancellationRequested;
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

		private async void buttonImport_Click(object sender, EventArgs e)
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(textBoxMeshFile.Text));

			var logger = new MemoryLogger();
			SolutionHub solutionHub = null;
			try
			{
				currentOperationCancellationTokenSource = new CancellationTokenSource();

				isImportOperationRunning = true;
				updateUI();

				string[] resultFiles = textBoxResultFiles.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				IEnumerable<AnalysisResult> analysisResults = new[]
					{
						new AnalysisResult
						{
							TimeStep = null, // TODO: use explicit time steps for .vtu
							MeshRecordNames = new[] { textBoxMeshFile.Text.RemoveQuotes() },
							DataRecordNames = resultFiles.Select(filename => filename.RemoveQuotes()).ToArray()
						}
					};
				string projectName = textBoxProjectName.Text;
				string location = textBoxLocation.Text;
				string solutionDirectory;
				if (checkBoxCreateDirectoryForSolution.Checked)
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
					await Task.Run(() => solutionHub.Import(keyTimeSteps, compressionParameters, gaussPointsExtrapolationStrategyName, cancellationToken: currentOperationCancellationTokenSource.Token));
					SolutionFileName = solutionHub.GetSolutionDescription().Location;
				}

				DialogResult = DialogResult.OK;
				this.Close();
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
				new ExceptionReportForm("Import FEM results", ex, logger).ShowDialog();
			}
			finally
			{
				isImportOperationRunning = false;
				updateUI();
			}
		}

		private string buildGaussPointsExtrapolationStrategyName()
		{
			return comboBoxGaussPointExtrapolationStrategy.SelectedItem as string;
		}

		private void buttonChooseMeshFile_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = SceneFacade.ImportMeshFileFormatFilter,
				FilterIndex = 0,
				Multiselect = false
			};

			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxMeshFile.Text = openFileDialog.FileName.QuoteIfContainsWhiteSpace();
			}
		}

		private void buttonChooseResultFiles_Click(object sender, EventArgs e)
		{
			var openFileDialog = new OpenFileDialog
			{
				Filter = SceneFacade.ImportDataFileFormatFilter,
				FilterIndex = 0,
				Multiselect = true
			};

			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxResultFiles.Text = string.Join(" ", openFileDialog.FileNames.Select(filename => filename.QuoteIfContainsWhiteSpace()));
			}
		}

		private void textBoxMeshFile_TextChanged(object sender, EventArgs e)
		{
			textBoxProjectName.Text = Path.GetFileNameWithoutExtension(textBoxMeshFile.Text).MakeAlphanumeric();
		}

		private void textBoxProjectName_TextChanged(object sender, EventArgs e)
		{
			updateUI();
		}

		private void textBoxLocation_TextChanged(object sender, EventArgs e)
		{
			updateUI();
		}

		private void updateUI()
		{
			buttonImport.Enabled = !isImportOperationRunning && !string.IsNullOrWhiteSpace(textBoxMeshFile.Text) && !string.IsNullOrWhiteSpace(textBoxProjectName.Text) && !string.IsNullOrWhiteSpace(textBoxLocation.Text);
			tabControl.Enabled = !isImportOperationRunning;
		}

		private void buttonChooseSolutionDirectory_Click(object sender, EventArgs e)
		{
			var folderBrowserDialog = new FolderBrowserDialog
			{
				SelectedPath = SolutionHub.GetLocalStorageDefaultDirectory().Replace('/', '\\')
			};

			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxLocation.Text = folderBrowserDialog.SelectedPath;
			}
		}

		private void buttonClose_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}
	}
}
