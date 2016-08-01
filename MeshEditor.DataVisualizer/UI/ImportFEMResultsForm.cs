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

namespace MeshEditor.WinUI
{
	public partial class ImportFEMResultsForm : Form
	{
		public ImportFEMResultsForm()
		{
			InitializeComponent();
			comboBoxCompressionMethod.SelectedIndex = 0;
			radioButtonQuality.Checked = true;
			trackBarCompressionFactor.Value = 95;

			// update state of UI
			updateUI();
		}

		public int? NewSolutionId { get; private set; }

		bool isImportOperationRunning;

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

				int solutionId = await createNewSolutionAsync(analysisResultGroupLengths, analysisResultRecordNames, projectName, logger);
				bool success = await importResultFilesAsync(analysisResultGroupLengths, analysisResultRecordNames, solutionId, buildCompressionParameters(), buildKeyTimeSteps());
				if (success)
				{
					NewSolutionId = solutionId;
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

		private async Task<int> createNewSolutionAsync(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames, string projectName, ILogger logger)
		{
			int solutionId = await getUniqueSolutionIdAsync(logger);

			var solutionHub = SolutionHub.CreateLocal(solutionId, logger);
			solutionHub.Create(analysisResultGroupLengths, analysisResultRecordNames, projectName); // TODO: make it async

			return solutionId;

			//int returnCode = await LayerManagerProcessInvokeService.Invoke($"create -l {string.Join(" ", analysisResultGroupLengths)} -r {string.Join(" ", analysisResultRecordNames)} --solution {solutionId} --verbose --pressanykey");
			//return returnCode == 0 ? solutionId : (int?)null;
		}

		private async Task<bool> importResultFilesAsync(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames, int solutionId, IEnumerable<string> compressionParameters, IEnumerable<string> keyTimeSteps)
		{
			//solutionHub.Import(analysisResultGroupLengths, analysisResultRecordNames, keyTimeSteps: Enumerable.Empty<double>(), compressionParameters: Enumerable.Empty<string>());

			string arguments = $"import -l {string.Join(" ", analysisResultGroupLengths)} -r {string.Join(" ", analysisResultRecordNames.Select(recordName => recordName.QuoteIfContainsWhiteSpace()))} --solution {solutionId} --verbose --pressanykey";
			if (compressionParameters.Any())
			{
				arguments += " -c " + string.Join(" ", compressionParameters);
			}
			if (keyTimeSteps.Any())
			{
				arguments += " -k " + string.Join(" ", keyTimeSteps);
			}
			int returnCode = await LayerManagerProcessInvokeService.Invoke(arguments);
			return returnCode == 0;
		}

		private static async Task<int> getUniqueSolutionIdAsync(ILogger logger)
		{
			var allSolutionsInDefaultDirectory = await SolutionHub.EnumerateAllLocalSolutionsAsync(CancellationToken.None, logger);
			return 1 + allSolutionsInDefaultDirectory.Select(solution => solution.Id).DefaultIfEmpty().Max();
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

		private void trackBarCompressionFactor_ValueChanged(object sender, EventArgs e)
		{
			labelCompressionFactor.Text = $"Compression factor: {trackBarCompressionFactor.Value} %";
			updateUI();
		}

		private void updateUI()
		{
			groupBoxSVDCompressionParameters.Enabled = comboBoxCompressionMethod.SelectedIndex > 0;
			textBoxKeyTimeSteps.Enabled = checkBoxMergeTimeSteps.Checked;
			buttonImport.Enabled = !isImportOperationRunning && !string.IsNullOrWhiteSpace(textBoxMeshFile.Text);
			tabControl.Enabled = !isImportOperationRunning;
		}
	}
}
