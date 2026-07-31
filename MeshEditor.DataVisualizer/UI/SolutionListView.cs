using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class SolutionListView : UserControl
	{
		public SolutionListView()
		{
			InitializeComponent();
		}

		public ISolutionInfo SelectedSolution
		{
			get
			{
				var selectedListViewItem = listView.SelectedItems.Cast<ListViewItem>().SingleOrDefault();
				return selectedListViewItem?.Tag as ISolutionInfo;
			}
		}

		[Browsable(true)]
		[Category("Behavior")]
		public event EventHandler SelectedSolutionChanged;

		[Browsable(true)]
		[Category("Behavior")]
		public event EventHandler SolutionListDoubleClick;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string Notification
		{
			get { return labelNotification.Text; }
			set
			{
				labelNotification.Text = value ?? "";
				labelNotification.Visible = !string.IsNullOrWhiteSpace(value);
			}
		}

		public void SetSolutions(IEnumerable<ISolutionInfo> solutions)
		{
			var groups = from solution in solutions
						 orderby solution.ProjectName, solution.Id
						 group solution by solution.ProjectName into g
						 select g;

			listView.Items.Clear();
			listView.Groups.Clear();

			foreach (var group in groups)
			{
				var listViewGroup = new ListViewGroup(group.Key);
				listView.Groups.Add(listViewGroup);

				foreach (var solution in group)
				{
					var listViewItem = new ListViewItem(new[] { solution.ProjectName, solution.Id.ToString(), solution.Location }, listViewGroup)
					{
						Tag = solution,
						ToolTipText = $"Project name: {solution.ProjectName}, Solution Id: {solution.Id}"
					};
					listView.Items.Add(listViewItem);
				}
			}

			Notification = (listView.Items.Count == 0) ? "No solutions found" : null;
		}

		private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			SelectedSolutionChanged?.Invoke(this, EventArgs.Empty);
		}

		private void listView_DoubleClick(object sender, EventArgs e)
		{
			SolutionListDoubleClick?.Invoke(this, EventArgs.Empty);
		}
	}
}
