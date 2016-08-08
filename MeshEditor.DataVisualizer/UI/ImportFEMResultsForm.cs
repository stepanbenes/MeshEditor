using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager;
using MeshEditor.LayerManager.Common;
using System.IO;
using MeshEditor.DataVisualizer.Services;
using System.Threading;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class ImportFEMResultsForm : Form
	{
		bool isImportOperationRunning;

		public ImportFEMResultsForm()
		{
			InitializeComponent();
			comboBoxCompressionMethod.SelectedIndex = 0;
			radioButtonQuality.Checked = true;
			trackBarCompressionFactor.Value = 95;
			comboBoxGaussPointExtrapolationStrategy.SelectedIndex = 0;
			textBoxLocation.Text = SolutionHub.GetLocalStorageDefaultDirectory();

			// update state of UI
			updateUI();
		}

		public int? SolutionId { get; private set; }
		public string SolutionDirectory { get; private set; }

		private async void buttonImport_Click(object sender, EventArgs e)
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(textBoxMeshFile.Text));

			var logger = new MemoryLogger();
			try
			{
				isImportOperationRunning = true;
				updateUI();

				string[] resultFiles = textBoxResultFiles.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				IEnumerable<int> analysisResultGroupLengths = new[] { resultFiles.Length + 1 };
				IEnumerable<string> analysisResultRecordNames = resultFiles.Prepend(textBoxMeshFile.Text).Select(filename => filename.RemoveQuotes());
				string projectName = textBoxProjectName.Text;
				string location = textBoxLocation.Text;
				bool createDirectoryForSolution = checkBoxCreateDirectoryForSolution.Checked;

				await createLocationForSolutionAsync(location, createDirectoryForSolution, projectName, logger);
				createNewSolution(SolutionId.Value, SolutionDirectory, analysisResultGroupLengths, analysisResultRecordNames, projectName, logger);
				bool success = await importResultFilesAsync(analysisResultGroupLengths, analysisResultRecordNames, SolutionId.Value, SolutionDirectory, buildCompressionParameters(), buildKeyTimeSteps(), buildGaussPointsExtrapolationStrategyName());
				if (success)
				{
					DialogResult = DialogResult.OK; // close dialog
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

		private IEnumerable<string> buildCompressionParameters()
		{
			List<string> parameters = new List<string>();
			if (comboBoxCompressionMethod.SelectedIndex > 0)
			{
				parameters.Add((string)comboBoxCompressionMethod.SelectedItem);

				// add compression factor parameters
				if (radioButtonQuality.Checked)
				{
					parameters.Add("quality");
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

		private IEnumerable<string> buildKeyTimeSteps()
		{
			if (checkBoxMergeTimeSteps.Checked)
			{
				string[] keyTimes = textBoxKeyTimeSteps.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (!keyTimes.Any())
					return Enumerable.Repeat("Infinity", 1);
				return keyTimes;
			}
			else
			{
				return Enumerable.Empty<string>();
			}
		}

		private string buildGaussPointsExtrapolationStrategyName()
		{
			return comboBoxGaussPointExtrapolationStrategy.SelectedItem as string;
		}

		private static void createNewSolution(int solutionId, string solutionDirectory, IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames, string projectName, ILogger logger)
		{
			//int returnCode = await LayerManagerProcessInvokeService.Invoke($"create -l {string.Join(" ", analysisResultGroupLengths)} -r {string.Join(" ", analysisResultRecordNames)} --solution {solutionId} --verbose --pressanykey");

			var solutionHub = SolutionHub.CreateLocal(solutionId, solutionDirectory, logger);
			solutionHub.Create(analysisResultGroupLengths, analysisResultRecordNames, projectName); // TODO: make it async
		}

		private static async Task<bool> importResultFilesAsync(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames, int solutionId, string solutionDirectory, IEnumerable<string> compressionParameters, IEnumerable<string> keyTimeSteps, string gaussPointsExtrapolationStrategyName)
		{
			//solutionHub.Import(analysisResultGroupLengths, analysisResultRecordNames, keyTimeSteps: Enumerable.Empty<double>(), compressionParameters: Enumerable.Empty<string>());

			StringBuilder arguments = new StringBuilder();

			arguments.Append("import");
			arguments.Append(" -l " + string.Join(" ", analysisResultGroupLengths));
			arguments.Append(" -r " + string.Join(" ", analysisResultRecordNames.Select(recordName => recordName.QuoteIfContainsWhiteSpace())));
			arguments.Append(" --solution " + solutionId);
			arguments.Append(" --solutiondirectory " + solutionDirectory.QuoteIfContainsWhiteSpace());
			if (compressionParameters.Any())
			{
				arguments.Append(" -c " + string.Join(" ", compressionParameters));
			}
			if (keyTimeSteps.Any())
			{
				arguments.Append(" -k " + string.Join(" ", keyTimeSteps));
			}
			if (gaussPointsExtrapolationStrategyName != null)
			{
				arguments.Append(" --gpextrapolation " + gaussPointsExtrapolationStrategyName);
			}
			arguments.Append(" --verbose");
			arguments.Append(" --pressanykey");

			int returnCode = await LayerManagerProcessInvokeService.Invoke(arguments.ToString());
			return returnCode == 0;
		}

		private async Task createLocationForSolutionAsync(string location, bool createDirectoryForSolution, string projectName, ILogger logger)
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(location));
			string solutionDirectory;
			if (createDirectoryForSolution)
			{
				Debug.Assert(!string.IsNullOrWhiteSpace(projectName));
				solutionDirectory = Path.Combine(location, projectName.MakeAlphanumericFilename());
				if (!Directory.Exists(solutionDirectory))
				{
					Directory.CreateDirectory(solutionDirectory);
				}
			}
			else
			{
				solutionDirectory = location;
			}
			Debug.Assert(Directory.Exists(solutionDirectory));

			var allSolutionsInDefaultDirectory = await SolutionHub.EnumerateAllLocalSolutionsAsync(solutionDirectory, CancellationToken.None, logger);

			SolutionId = 1 + allSolutionsInDefaultDirectory.Select(solution => solution.Id).DefaultIfEmpty().Max();
			SolutionDirectory = solutionDirectory;
		}

		private void buttonChooseMeshFile_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = MeshEditor.CoreInterface.SceneFacade.ImportMeshFileFormatFilter;
			openFileDialog.FilterIndex = 0;
			openFileDialog.Multiselect = false;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxMeshFile.Text = openFileDialog.FileName.QuoteIfContainsWhiteSpace();
				textBoxProjectName.Text = Path.GetFileNameWithoutExtension(openFileDialog.FileName).MakeAlphanumericFilename();
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
			updateUI();
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
