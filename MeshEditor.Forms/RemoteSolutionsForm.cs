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

namespace MeshEditor.WinUI
{
	public struct SolutionThumbnail
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

	public partial class RemoteSolutionsForm : Form
	{
		public RemoteSolutionsForm(IEnumerable<SolutionThumbnail> solutions)
		{
			InitializeComponent();
			listBoxSolutions_SelectedIndexChanged(null, null);

			foreach (var solution in solutions)
			{
				listBoxSolutions.Items.Add(solution);
			}
		}

		public int? SelectedSolutionId => (listBoxSolutions.SelectedItem as SolutionThumbnail?)?.SolutionId;

		private void listBoxSolutions_SelectedIndexChanged(object sender, EventArgs e)
		{
			buttonOk.Enabled = buttonOpenInBrowser.Enabled = listBoxSolutions.SelectedItem != null;
		}

		private void buttonOpenInBrowser_Click(object sender, EventArgs e)
		{
			var url = $"http://mesheditor.azurewebsites.net/postprocess/{SelectedSolutionId.Value}";
			Process.Start(url);
		}
	}
}
