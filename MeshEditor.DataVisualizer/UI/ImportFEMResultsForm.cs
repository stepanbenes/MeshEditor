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

namespace MeshEditor.DataVisualizer.UI
{
	public partial class ImportFEMResultsForm : Form
	{
		bool isImportOperationRunning;
		readonly LongOpNotifier longOpNotifier;

		public ImportFEMResultsForm(LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.longOpNotifier = longOpNotifier;
			comboBoxCompressionMethod.SelectedIndex = 0;
			radioButtonQuality.Checked = true;
			trackBarCompressionFactor.Value = 95;
			comboBoxGaussPointExtrapolationStrategy.SelectedIndex = 0;
			textBoxLocation.Text = SolutionHub.GetLocalStorageDefaultDirectory();

			// update state of UI
			updateUI();
		}

		public string SolutionFileName { get; private set; }

		private async void buttonImport_Click(object sender, EventArgs e)
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(textBoxMeshFile.Text));

			var logger = new MemoryLogger();
			try
			{
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

				var keyTimeSteps = buildKeyTimeSteps();
				var compressionParameters = buildCompressionParameters();
				var gaussPointsExtrapolationStrategyName = buildGaussPointsExtrapolationStrategyName();

				using (longOpNotifier.Begin("Importing FEM results", isCancellable: false, logger: logger))
				{
					var solutionHub = SolutionHub.CreateNewLocal(solutionDirectory, analysisResults, projectName, logger);
					await Task.Run(() => solutionHub.Import(keyTimeSteps, compressionParameters, gaussPointsExtrapolationStrategyName));
					SolutionFileName = solutionHub.GetSolutionDescription().Location;
				}

				DialogResult = DialogResult.OK; // close dialog
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

		private IEnumerable<string> buildCompressionParameters()
		{
			List<string> parameters = new List<string>();
			if (comboBoxCompressionMethod.SelectedIndex > 0)
			{
				parameters.Add((string)comboBoxCompressionMethod.SelectedItem);

				// add compression factor parameters
				if (radioButtonQuality.Checked)
				{
					parameters.Add("error");
					parameters.Add((trackBarCompressionFactor.Value * 0.01).ToString());
				}
				else if (radioButtonSize.Checked)
				{
					parameters.Add("size");
					parameters.Add((trackBarCompressionFactor.Value * 0.01).ToString());
				}
				if (checkBoxSVDParameterRandomized.Checked)
				{
					parameters.Add("randomized");
				}
			}
			return parameters;
		}

		private IEnumerable<double> buildKeyTimeSteps()
		{
			if (checkBoxMergeTimeSteps.Checked)
			{
				string[] tokens = textBoxKeyTimeSteps.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (!tokens.Any())
					return Enumerable.Repeat(double.PositiveInfinity, 1);
				return tokens.Select(token => double.Parse(token)); // TODO: handle parsing errors
			}
			else
			{
				return Enumerable.Empty<double>();
			}
		}

		private string buildGaussPointsExtrapolationStrategyName()
		{
			return comboBoxGaussPointExtrapolationStrategy.SelectedItem as string;
		}

		//private static async Task<bool> importResultFilesAsync(string solutionFileName, IEnumerable<string> compressionParameters, IEnumerable<string> keyTimeSteps, string gaussPointsExtrapolationStrategyName)
		//{
		//	StringBuilder arguments = new StringBuilder();

		//	arguments.Append("import");
		//	arguments.Append(" --solution " + solutionFileName.QuoteIfContainsWhiteSpace());
		//	if (compressionParameters.Any())
		//	{
		//		arguments.Append(" -c " + string.Join(" ", compressionParameters));
		//	}
		//	if (keyTimeSteps.Any())
		//	{
		//		arguments.Append(" -k " + string.Join(" ", keyTimeSteps));
		//	}
		//	if (gaussPointsExtrapolationStrategyName != null)
		//	{
		//		arguments.Append(" --gpextrapolation " + gaussPointsExtrapolationStrategyName);
		//	}
		//	arguments.Append(" --verbose");
		//	arguments.Append(" --pressanykey");

		//	var exitCode = await LayerManagerProcessInvokeService.Invoke(arguments.ToString());
		//	return exitCode == 0;
		//}

		private void buttonChooseMeshFile_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = MeshEditor.CoreInterface.SceneFacade.ImportMeshFileFormatFilter;
			openFileDialog.FilterIndex = 0;
			openFileDialog.Multiselect = false;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxMeshFile.Text = openFileDialog.FileName.QuoteIfContainsWhiteSpace();
			}
		}

		private void buttonChooseResultFiles_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = MeshEditor.CoreInterface.SceneFacade.ImportDataFileFormatFilter;
			openFileDialog.FilterIndex = 0;
			openFileDialog.Multiselect = true;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxResultFiles.Text = string.Join(" ", openFileDialog.FileNames.Select(filename => filename.QuoteIfContainsWhiteSpace()));
			}
		}

		private void comboBoxCompressionMethod_SelectedIndexChanged(object sender, EventArgs e)
		{
			checkBoxMergeTimeSteps.Checked = comboBoxCompressionMethod.SelectedIndex > 0;
			updateUI();
		}

		private void checkBoxMergeTimeSteps_CheckedChanged(object sender, EventArgs e)
		{
			updateUI();
		}

		private void textBoxMeshFile_TextChanged(object sender, EventArgs e)
		{
			textBoxProjectName.Text = Path.GetFileNameWithoutExtension(textBoxMeshFile.Text).MakeAlphanumeric();
			//updateUI();
		}

		private void textBoxProjectName_TextChanged(object sender, EventArgs e)
		{
			updateUI();
		}

		private void textBoxLocation_TextChanged(object sender, EventArgs e)
		{
			updateUI();
		}

		private void trackBarCompressionFactor_ValueChanged(object sender, EventArgs e)
		{
			labelCompressionFactor.Text = $"Compression factor: {trackBarCompressionFactor.Value} %";
			updateUI();
		}

		private void updateUI()
		{
			groupBoxSVDCompressionParameters.Enabled = comboBoxCompressionMethod.SelectedIndex > 0;
			textBoxKeyTimeSteps.Enabled = checkBoxMergeTimeSteps.Checked;
			buttonImport.Enabled = !isImportOperationRunning && !string.IsNullOrWhiteSpace(textBoxMeshFile.Text) && !string.IsNullOrWhiteSpace(textBoxProjectName.Text) && !string.IsNullOrWhiteSpace(textBoxLocation.Text);
			tabControl.Enabled = !isImportOperationRunning;
		}

		private void buttonChooseSolutionDirectory_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			folderBrowserDialog.SelectedPath = SolutionHub.GetLocalStorageDefaultDirectory().Replace('/', '\\');
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxLocation.Text = folderBrowserDialog.SelectedPath;
			}
		}
	}
}
