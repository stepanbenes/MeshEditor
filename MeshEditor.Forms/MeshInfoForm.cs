using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MeshEditor.CoreInterface;
using MeshEditor.Graphics;
using MeshEditor.Data;
using System.Diagnostics;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// Tento formular zobrazi uzivateli zakladni informace o siti 
	/// (pocet uzlu, prvku a dalsi parametry site). 
	/// Dale umoznuje priradit k jiz pouzitym cislum vlastnosti komentar, jenz je charakterizuje.
	/// </summary>
	public partial class MeshInfoForm : Form
	{
		private SceneFacade sceneFacade;
		private BackgroundWorker nodeCountComputer;
		private LongOpNotifier longOpNotifier;

		private static int lastSelectedTabIndex = 1; // default tab is "Property descriptions"

		private static readonly int COMMAND_ROW_HEIGHT_FACTOR = 13;
		private static readonly int COMMAND_ROW_HEIGHT_OFFSET = 8;

		public MeshInfoForm(SceneFacade sceneFacade, LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.sceneFacade = sceneFacade;
			this.longOpNotifier = longOpNotifier;
			init();
			initNodeCountComputer();
		}

		private void initNodeCountComputer()
		{
			nodeCountComputer = new BackgroundWorker();
			nodeCountComputer.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				e.Result = sceneFacade.GetValue(AvailableValue.NodeCount);
			};
			nodeCountComputer.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Result is int)
					labelNodeCount.Text = "Node count: " + e.Result;
			};
			nodeCountComputer.RunWorkerAsync();
		}

		private void init()
		{
			tabControl.SelectedIndex = lastSelectedTabIndex;

			if (sceneFacade.ContainsMesh)
			{
				MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;

				if (statistics != null)
				{
					fillPropertyDescriptionsListView(statistics);
					// ------------------------------------------------------------------

					//histogramViewer.DataWidthChanged += delegate
					//{
					//    firstBorderLimitTrackBar.Width = histogramViewer.DataWidth + 10;
					//    secondBorderLimitTrackBar.Width = histogramViewer.DataWidth + 10;
					//};

					firstBorderLimitTrackBar.Value = (int)statistics.SoftBorderLimit;
					secondBorderLimitTrackBar.Value = (int)statistics.HardBorderLimit;
					if (statistics.EdgeAnglesHistogram != null)
						histogramViewer.SetHistogram(statistics.EdgeAnglesHistogram);
				}

				firstBorderLimitLabel.Text = "First border limit: " + firstBorderLimitTrackBar.Value + "°";
				secondBorderLimitLabel.Text = "Second border limit: " + secondBorderLimitTrackBar.Value + "°";

			}
			
			initBasicMeshInfo();
		}

		private void fillPropertyDescriptionsListView(MeshStatistics statistics)
		{
			foreach (PropertyEntityPair propertyTargetPair in statistics.GetAllPropertyEntityPairs())
			{
				// property
				Property property = propertyTargetPair.Property;

				// target entity
				EntityType propertyTarget = propertyTargetPair.EntityType;

				// command
				StringBuilder commandsText = new StringBuilder();
				List<PropertyCommand> commands = null;
				if (statistics.PropertyCommands.TryGetValue(propertyTargetPair, out commands))
				{
					foreach (PropertyCommand command in commands)
						commandsText.AppendLine(command.ToString());
				}

				// comment
				string comment;
				statistics.PropertyComments.TryGetValue(property, out comment);

				// add row
				dataGridViewPropertyDescriptions.Rows.Add(property, propertyTarget, commandsText.ToString().Trim(), comment);

				if (commands != null && commands.Count > 1)
					dataGridViewPropertyDescriptions.Rows[dataGridViewPropertyDescriptions.Rows.Count - 1].Height = commands.Count * COMMAND_ROW_HEIGHT_FACTOR + COMMAND_ROW_HEIGHT_OFFSET;
			}

			// sort list by property value
			dataGridViewPropertyDescriptions.Sort(dataGridViewPropertyDescriptions.Columns["PropertyColumn"], ListSortDirection.Ascending);
		}

		private void initBasicMeshInfo()
		{
			if (!sceneFacade.ContainsMesh)
			{
				foreach (Control c in groupBoxBasicInfo.Controls)
					c.Enabled = false;
				return;
			}

			bool meshIsCutted = (bool)sceneFacade.GetValue(AvailableValue.MeshHasHiddenElements);
			if (meshIsCutted)
				groupBoxBasicInfo.Text += " (after cut)";
			//labelBasicInfoTitle.Text = (meshIsCutted) ? "Mesh after cut characteristics" : "Mesh characteristics";

			// ---------------------------------------
			labelNodeCount.Text = "Node count: ...";// + sceneFacade.GetValue(AvailableValue.NodeCount);

			labelElementCount.Text = "Element count: " + sceneFacade.GetValue(AvailableValue.ElementCount);
			labelBeamCount.Text = "(Beam count: " + sceneFacade.GetValue(AvailableValue.BeamCount) + ")";

			labelFaceCount.Text = "Face count: " + sceneFacade.GetValue(AvailableValue.FaceCount);
			labelEdgeCount.Text = "Edge count: " + sceneFacade.GetValue(AvailableValue.EdgeCount);
			
		}

		private void firstBorderLimitTrackBar_ValueChanged(object sender, EventArgs e)
		{
			if (sceneFacade.ContainsMesh)
			{
				firstBorderLimitLabel.Text = "First border limit: " + firstBorderLimitTrackBar.Value + "°";
				if (firstBorderLimitTrackBar.Value > secondBorderLimitTrackBar.Value)
					secondBorderLimitTrackBar.Value = firstBorderLimitTrackBar.Value;
			}
			histogramViewer.FirstLimit = firstBorderLimitTrackBar.Value;
			buttonApply.Focus();
		}

		private void secondBorderLimitTrackBar_ValueChanged(object sender, EventArgs e)
		{
			if (sceneFacade.ContainsMesh)
			{
				secondBorderLimitLabel.Text = "Second border limit: " + secondBorderLimitTrackBar.Value + "°";
				if (secondBorderLimitTrackBar.Value < firstBorderLimitTrackBar.Value)
					firstBorderLimitTrackBar.Value = secondBorderLimitTrackBar.Value;
			}
			histogramViewer.SecondLimit = secondBorderLimitTrackBar.Value;
			buttonApply.Focus();
		}

		private void buttonApply_Click(object sender, EventArgs e)
		{
			if (sceneFacade.ContainsMesh)
			{
				MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;

				if (statistics != null)
				{
					statistics.SetBorderLimits((float)firstBorderLimitTrackBar.Value, (float)secondBorderLimitTrackBar.Value);
					sceneFacade.PerformAction(AvailableAction.RecreateBuffers);
				}

				// -----------------------------------------------
				using (longOpNotifier.Begin("Updating border limits"))
				{
					Cursor temp = this.Cursor;
					this.Cursor = Cursors.WaitCursor;
					// -----------------------------------------------
					sceneFacade.PerformAction(AvailableAction.Refresh);
					// -----------------------------------------------
					this.Cursor = temp;
				}
				// -----------------------------------------------
			}
		}

		private void buttonClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void buttonEditPropertyCommands_Click(object sender, EventArgs e)
		{
			editPropertyCommands();
		}

		//private void listViewPropertyDescriptions_DoubleClick(object sender, EventArgs e)
		//{
		//    editPropertyCommands();
		//}

		//private void listViewPropertyDescriptions_KeyDown(object sender, KeyEventArgs e)
		//{
		//    if (e.KeyCode == Keys.Return)
		//        editPropertyCommands();
		//}

		private void editPropertyCommands()
		{
			if (dataGridViewPropertyDescriptions.SelectedRows.Count == 0)
				return;

			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (statistics == null)
				return;

			DataGridViewRow selectedRow = dataGridViewPropertyDescriptions.SelectedRows[0];
			Property property = new Property(int.Parse(getTextInDataCell(selectedRow, "PropertyColumn")));
			EntityType propertyTarget = (EntityType)Enum.Parse(typeof(EntityType), getTextInDataCell(selectedRow, "TargetEntityColumn"), /*ignoreCase: */ true);

			PropertyEntityPair propertyTargetPair = new PropertyEntityPair(property, propertyTarget);

			List<PropertyCommand> commands = null;
			statistics.PropertyCommands.TryGetValue(propertyTargetPair, out commands);

			PropertyCommandForm propertyCommandForm = new PropertyCommandForm(propertyTargetPair, commands);
			if (propertyCommandForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				// save command
				if (propertyCommandForm.Commands == null || propertyCommandForm.Commands.Count == 0)
					statistics.PropertyCommands.Remove(propertyTargetPair);
				else
					statistics.PropertyCommands[propertyTargetPair] = propertyCommandForm.Commands;

				string commandString = string.Empty;
				if (propertyCommandForm.Commands != null)
				{
					StringBuilder commandText = new StringBuilder();
					foreach (PropertyCommand command in propertyCommandForm.Commands)
						commandText.AppendLine(command.ToString());
					commandString = commandText.ToString().Trim();
				}

				if (getTextInDataCell(selectedRow, "CommandsColumn") != commandString)
				{
					selectedRow.Cells["CommandsColumn"].Value = commandString;

					if (propertyCommandForm.Commands != null && propertyCommandForm.Commands.Count > 1)
						selectedRow.Height = propertyCommandForm.Commands.Count * COMMAND_ROW_HEIGHT_FACTOR + COMMAND_ROW_HEIGHT_OFFSET;

					// nastavit sit do stavu, ktery vyzaduje ulozeni zmen
					sceneFacade.SetValue(AvailableValue.UnsavedChangesInMesh, true);
				}
			}
		}

		private string getTextInDataCell(DataGridViewRow row, string columnName)
		{
			object value = row.Cells[columnName].Value;
			return value != null ? value.ToString() : string.Empty;
		}

		private void editPropertyComment()
		{
			if (dataGridViewPropertyDescriptions.SelectedRows.Count == 0)
				return;
			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (statistics == null)
				return;

			DataGridViewRow selectedRow = dataGridViewPropertyDescriptions.SelectedRows[0];
			Property property = new Property(int.Parse(getTextInDataCell(selectedRow, "PropertyColumn")));
			string initialText = getTextInDataCell(selectedRow, "CommentColumn");

			InputValueForm inputValueForm = new InputValueForm("Set description of property", "Property " + property + ":", initialText);
			inputValueForm.Width = 500;
			if (inputValueForm.ShowDialog() == DialogResult.OK)
			{
				// save comment
				if (string.IsNullOrEmpty(inputValueForm.InputValue))
					statistics.PropertyComments.Remove(property);
				else
					statistics.PropertyComments[property] = inputValueForm.InputValue;
				if (getTextInDataCell(selectedRow, "CommentColumn") != inputValueForm.InputValue)
				{
					//subItems[3].Text = inputValueForm.InputValue;
					setAllCommentsOfProperty(property, inputValueForm.InputValue); // update all lines with this property

					// nastavit sit do stavu, ktery vyzaduje ulozeni zmen
					sceneFacade.SetValue(AvailableValue.UnsavedChangesInMesh, true);
				}
			}
		}

		private void setAllCommentsOfProperty(Property property, string comment)
		{
			string propertyString = property.ToString();
			foreach (DataGridViewRow row in dataGridViewPropertyDescriptions.Rows)
			{
				if (getTextInDataCell(row, "PropertyColumn") == propertyString)
					row.Cells["CommentColumn"].Value = comment;
			}
		}

		private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			lastSelectedTabIndex = tabControl.SelectedIndex;
		}

		private void buttonEditComment_Click(object sender, EventArgs e)
		{
			editPropertyComment();
		}

		private void dataGridViewPropertyDescriptions_DoubleClick(object sender, EventArgs e)
		{
			editPropertyCommands();
		}

		private void dataGridViewPropertyDescriptions_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return)
			{
				e.Handled = true;
				editPropertyCommands();
			}
		}

	}
}
