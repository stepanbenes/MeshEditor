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

namespace MeshEditor.WinUI
{
	public partial class ImportFEMResultsForm : Form
	{
		public ImportFEMResultsForm()
		{
			InitializeComponent();
		}

		public int? NewSolutionId { get; private set; }

		private async void buttonImport_Click(object sender, EventArgs e)
		{
			// TODO: check pre-conditions

			Debug.Assert(!string.IsNullOrWhiteSpace(textBoxMeshFile.Text));
			//Debug.Assert(!string.IsNullOrWhiteSpace(textBoxResultFiles.Text));

			buttonImport.Enabled = false;
			tabControl1.Enabled = false;
			{
				string[] resultFiles = textBoxResultFiles.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

				IEnumerable<int> analysisResultGroupLengths = new[] { resultFiles.Length + 1 };
				IEnumerable<string> analysisResultRecordNames = resultFiles.Prepend(textBoxMeshFile.Text);
				string projectName = textBoxProjectName.Text;

				int? solutionId = await createNewSolution(analysisResultGroupLengths, analysisResultRecordNames, projectName);
				if (solutionId.HasValue)
				{
					bool success = await importResultFiles(analysisResultGroupLengths, analysisResultRecordNames, solutionId.Value);
					if (success)
					{
						NewSolutionId = solutionId;
						DialogResult = DialogResult.OK; // close dialog
					}
				}
			}
			buttonImport.Enabled = true;
			tabControl1.Enabled = true;
		}

		private async Task<int?> createNewSolution(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames, string projectName)
		{
			int solutionId = getUniqueSolutionId();

			//var solutionHub = SolutionHub.CreateLocal(solutionId);
			//solutionHub.Create(analysisResultGroupLengths, analysisResultRecordNames, projectName);

			int returnCode = await LayerManagerProcessInvokeService.Invoke($"create -l {string.Join(" ", analysisResultGroupLengths)} -r {string.Join(" ", analysisResultRecordNames)} --solution {solutionId} --verbose"); // TODO: quote record names with white space
			return returnCode == 0 ? solutionId : (int?)null;
		}

		private async Task<bool> importResultFiles(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames, int solutionId)
		{
			//solutionHub.Import(analysisResultGroupLengths, analysisResultRecordNames, keyTimeSteps: Enumerable.Empty<double>(), compressionParameters: Enumerable.Empty<string>());

			int returnCode = await LayerManagerProcessInvokeService.Invoke($"import -l {string.Join(" ", analysisResultGroupLengths)} -r {string.Join(" ", analysisResultRecordNames)} --solution {solutionId} --verbose"); // TODO: quote record names with white space
			return returnCode == 0;
		}

		private static int getUniqueSolutionId()
		{
			var allSolutionsInDefaultDirectory = SolutionHub.EnumerateAllLocalSolutions();
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
				textBoxMeshFile.Text = openFileDialog.FileName;
				if (string.IsNullOrEmpty(textBoxProjectName.Text)) // construct default project name
				{
					textBoxProjectName.Text = Path.GetFileNameWithoutExtension(openFileDialog.FileName).MakeAlphanumericFilename();
				}
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
				textBoxResultFiles.Text = string.Join(";", openFileDialog.FileNames);
			}
		}
	}
}
