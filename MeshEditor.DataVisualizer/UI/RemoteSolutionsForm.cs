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

namespace MeshEditor.WinUI
{
	public partial class RemoteSolutionsForm : Form
	{
		private struct SolutionThumbnail
		{
			public SolutionThumbnail(int solutionId, string projectName)
			{
				SolutionId = solutionId;
				ProjectName = projectName;
			}

			public int SolutionId { get; }
			public string ProjectName { get; }

			public override string ToString()
			{
				return $"Solution id: {SolutionId}, Project name: {ProjectName}";
			}
		}

		public RemoteSolutionsForm()
		{
			InitializeComponent();

			var solutions = SolutionHub.EnumerateAllRemoteSolutions();

			foreach (var solution in solutions.Select(s => new SolutionThumbnail(s.Id, s.ProjectName)))
			{
				listBoxSolutions.Items.Add(solution);
			}

			listBoxSolutions_SelectedIndexChanged(null, null);
		}

		public int? SelectedSolutionId => (listBoxSolutions.SelectedItem as SolutionThumbnail?)?.SolutionId;

		private void listBoxSolutions_SelectedIndexChanged(object sender, EventArgs e)
		{
			buttonOk.Enabled = buttonOpenInBrowser.Enabled = listBoxSolutions.SelectedItem != null;
		}

		private void buttonOpenInBrowser_Click(object sender, EventArgs e)
		{
			var solutionId = SelectedSolutionId;
			if (solutionId.HasValue)
			{
				var url = $"http://mesheditor.azurewebsites.net/postprocess/{solutionId.Value}";
				Process.Start(url);
			}
		}

		private void listBoxSolutions_DoubleClick(object sender, EventArgs e)
		{
			if (listBoxSolutions.SelectedItem != null)
			{
				DialogResult = DialogResult.OK;
			}
		}
	}
}
