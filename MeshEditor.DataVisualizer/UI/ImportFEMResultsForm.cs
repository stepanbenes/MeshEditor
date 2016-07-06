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

namespace MeshEditor.WinUI
{
	public partial class ImportFEMResultsForm : Form
	{
		public ImportFEMResultsForm()
		{
			InitializeComponent();
		}

		public int? NewSolutionId { get; private set; }

		private void buttonImport_Click(object sender, EventArgs e)
		{
			// TODO: check pre-conditions

			Debug.Assert(!string.IsNullOrWhiteSpace(textBoxMeshFile.Text));
			Debug.Assert(!string.IsNullOrWhiteSpace(textBoxResultFiles.Text));

			string[] resultFiles = textBoxResultFiles.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

			IEnumerable<int> analysisResultGroupLengths = new[] { resultFiles.Length + 1 };
			IEnumerable<string> analysisResultRecordNames = resultFiles.Prepend(textBoxMeshFile.Text);
			string projectName = textBoxProjectName.Text;

			int solutionId = getUniqueSolutionId();
			var solutionHub = SolutionHub.CreateLocal(solutionId);
			solutionHub.Create(analysisResultGroupLengths, analysisResultRecordNames, projectName);
			solutionHub.Import(analysisResultGroupLengths, analysisResultRecordNames, keyTimeSteps: Enumerable.Empty<double>(), compressionParameters: Enumerable.Empty<string>());

			NewSolutionId = solutionId;
			DialogResult = DialogResult.OK; // close dialog
		}

		private static int getUniqueSolutionId()
		{
			var allSolutionsInDefaultDirectory = SolutionHub.EnumerateAllLocalSolutions();
			return 1 + allSolutionsInDefaultDirectory.Select(solution => solution.Id).DefaultIfEmpty().Max();
		}

		private void buttonChooseMeshFile_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Multiselect = false;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxMeshFile.Text = openFileDialog.FileName;
			}
		}

		private void buttonChooseResultFiles_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Multiselect = true;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxResultFiles.Text = string.Join(";", openFileDialog.FileNames);
			}
		}
	}
}
