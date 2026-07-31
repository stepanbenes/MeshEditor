using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MeshEditor.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MeshEditor.WinUI
{
	public partial class PropertyCommandForm : Form
	{
		private List<PropertyCommand> commands;
		private PropertyCommand selectedCommand;
		private Property property;
		private EntityType propertyTarget;
		private bool initializing;

		public PropertyCommandForm(PropertyEntityPair propertyTargetPair, List<PropertyCommand> savedCommands)
		{
			InitializeComponent();
			this.property = propertyTargetPair.Property;
			this.propertyTarget = propertyTargetPair.EntityType;
			List<PropertyCommand> clonedCommands = new List<PropertyCommand>();
			if (savedCommands != null)
			{
				foreach (PropertyCommand command in savedCommands)
					clonedCommands.Add(command.Clone());
			}
			init(clonedCommands);
		}
		
		public List<PropertyCommand> Commands
		{
			get { return commands; }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PropertyCommand SelectedCommand
		{
			get { return selectedCommand; }
			set
			{
				selectedCommand = value;
				onSelectedCommandChanged();
			}
		}

		private void onSelectedCommandChanged()
		{
			// update command combobox
			comboBoxAllCommands.SelectedIndex = commands.IndexOf(selectedCommand);
			bool enabled = selectedCommand != null;

			buttonRemoveCommand.Enabled = enabled;
			//labelSelectedCommand.Enabled = enabled;
			comboBoxAllCommands.Enabled = enabled;

			foreach (Control control in groupBoxCommandDescription.Controls)
				control.Enabled = enabled;
			groupBoxCommandDescription.Enabled = enabled;

			comboBoxPropertyType.SelectedItem = (SelectedCommand != null) ? SelectedCommand.Type.ToString() : null;
			
			setupDataGrid();

			if (SelectedCommand != null)
			{
				labelCommandPattern.Text = SelectedCommand.GetCommandPattern();
				labelFilledPattern.Text = SelectedCommand.FillPattern();
				labelResultText.Text = SelectedCommand.ToString();
			}
			else
			{
				labelCommandPattern.Text = labelFilledPattern.Text = labelResultText.Text = string.Empty;
			}
		}

		private void init(List<PropertyCommand> savedCommands)
		{
			Debug.Assert(savedCommands != null);
			try
			{
				initializing = true;

				this.commands = savedCommands;

				fillComboBoxPropertyType();

				foreach (PropertyCommand command in commands)
					addCommand(command);

				this.SelectedCommand = (commands.Count > 0) ? savedCommands[0] : null;
				labelPropertyNumber.Text = this.property.ToString() + " (" + this.propertyTarget.ToString() + ")";

				if (SelectedCommand != null)
				{
					comboBoxPropertyType.SelectedItem = SelectedCommand.Type.ToString();
				}
			}
			finally
			{
				initializing = false;
			}
		}

		private void fillComboBoxPropertyType()
		{
			comboBoxPropertyType.Items.Clear();

			// add all options:
			foreach (PropertyCommand.CommandType type in Enum.GetValues(typeof(PropertyCommand.CommandType)))
				comboBoxPropertyType.Items.Add(type.ToString());

			// filter options according to propertyTarget
			//PropertyCommand.PropertyType[] availablePropertyTypes = null;
			//switch (propertyTarget)
			//{
			//    case PropertyTarget.NodeVertex:
			//        availablePropertyTypes = new PropertyCommand.PropertyType[]
			//        {
			//            PropertyCommand.PropertyType.ndofn, 
			//            PropertyCommand.PropertyType.bocon, 
			//            PropertyCommand.PropertyType.dof_coupl,

			//            PropertyCommand.PropertyType.nod_tfunc,
			//            PropertyCommand.PropertyType.nod_crsec,
			//            PropertyCommand.PropertyType.nod_spring,
			//            PropertyCommand.PropertyType.nod_lcs,
			//            PropertyCommand.PropertyType.nod_load,
			//            PropertyCommand.PropertyType.nod_tdload,
			//            PropertyCommand.PropertyType.nod_inicond,
			//            PropertyCommand.PropertyType.nod_temper
			//        };
			//        break;
			//    case PropertyTarget.NodeEdge:
			//        availablePropertyTypes = new PropertyCommand.PropertyType[]
			//        {
			//            PropertyCommand.PropertyType.edge_load
			//        };
			//        break;
			//    case PropertyTarget.NodeSurface:
			//        availablePropertyTypes = new PropertyCommand.PropertyType[]
			//        {
			//            PropertyCommand.PropertyType.surf_load
			//        };
			//        break;
			//    case PropertyTarget.NodeVolume:
			//        availablePropertyTypes = new PropertyCommand.PropertyType[]
			//        {
			//            PropertyCommand.PropertyType.volume_load
			//        };
			//        break;
			//    case PropertyTarget.ElementEdge:
			//        availablePropertyTypes = new PropertyCommand.PropertyType[]
			//        {
			//            PropertyCommand.PropertyType.edge_load
			//        };
			//        break;
			//    case PropertyTarget.ElementSurface:
			//        availablePropertyTypes = new PropertyCommand.PropertyType[]
			//        {
			//            PropertyCommand.PropertyType.surf_load
			//        };
			//        break;
			//    case PropertyTarget.ElementVolume:
			//        availablePropertyTypes = new PropertyCommand.PropertyType[]
			//        {
			//            PropertyCommand.PropertyType.el_type,
			//            PropertyCommand.PropertyType.el_mat,
			//            PropertyCommand.PropertyType.el_crsec,
			//            PropertyCommand.PropertyType.el_lcs,
			//            PropertyCommand.PropertyType.el_load,
			//            PropertyCommand.PropertyType.el_tfunc,

			//            PropertyCommand.PropertyType.volume_load
			//        };
			//        break;
			//    default:
			//        availablePropertyTypes = new PropertyCommand.PropertyType[0];
			//        break;
			//}

			//foreach (PropertyCommand.PropertyType type in availablePropertyTypes) // fill comboBox items
			//    comboBoxPropertyType.Items.Add(type.ToString());
		}

		private void updateNameOfCommand(PropertyCommand command)
		{
			int index = commands.IndexOf(command);
			if (index >= 0)
			{
				string commandName = "Command " + command.Type;
				string updatedName = commandName;
				int namesakeCount = 1;
				for (int i = 0; i < comboBoxAllCommands.Items.Count; i++)
				{
					string name = comboBoxAllCommands.Items[i].ToString();
					if (name.StartsWith(updatedName))
					{
						namesakeCount++;
						updatedName = commandName + " " + namesakeCount;
						i = 0; // start from beginning
					}
				}
				comboBoxAllCommands.Items[index] = updatedName;
			}
		}

		private void addCommand(PropertyCommand command)
		{
			Debug.Assert(command != null);
			comboBoxAllCommands.Items.Add(string.Empty);
			updateNameOfCommand(command);
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK; // close dialog
		}

		private void comboBoxPropertyType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (initializing)
				return;

			Debug.Assert(SelectedCommand != null);

			SelectedCommand.Type = (PropertyCommand.CommandType)Enum.Parse(typeof(PropertyCommand.CommandType), comboBoxPropertyType.SelectedItem.ToString(), /*ignoreCase: */ true);
			updateNameOfCommand(SelectedCommand);

			SelectedCommand.VariableValueMap.Clear();
			SelectedCommand.VariableValueMap[PropertyCommand.PropertyNumberVariableName] = this.property.ToString();
			
			labelCommandPattern.Text = SelectedCommand.GetCommandPattern();
			labelFilledPattern.Text = SelectedCommand.FillPattern();
			labelResultText.Text = SelectedCommand.ToString();

			setupDataGrid();
		}
		
		private void setupDataGrid()
		{
			dataGridViewVariables.Rows.Clear();
			if (SelectedCommand == null)
				return;
			
			foreach (string variable in SelectedCommand.GetAllVariables())
			{
				// works on windows (.NET) ------------------------------------
				//DataGridViewRow row = new DataGridViewRow();
				//string value = null;
				//command.VariableValueMap.TryGetValue(variable, out value);
				//row.CreateCells(dataGridViewVariables, variable, value);
				//if (variable == PropertyCommand.PropertyNumberVariableName)
				//    row.Cells[1].ReadOnly = true;
				//dataGridViewVariables.Rows.Add(row);

				// workaround for mono: ----------------------------------------
				string value = null;
				SelectedCommand.VariableValueMap.TryGetValue(variable, out value);

				dataGridViewVariables.Rows.Add(variable, value);
				if (variable == PropertyCommand.PropertyNumberVariableName)
					dataGridViewVariables.Rows[dataGridViewVariables.RowCount - 1].Cells[1].ReadOnly = true;	
				// -------------------------------------------------------------
			}
			
			foreach (DataGridViewCell cell in dataGridViewVariables.SelectedCells) // unselect all
				cell.Selected = false;
			foreach (DataGridViewRow row in dataGridViewVariables.Rows) // select first editable cell
			{
				if (!row.Cells[1].ReadOnly)
				{
					row.Cells[1].Selected = true;
					dataGridViewVariables.CurrentCell = row.Cells[1];
					break;
				}
			}

			//movePropertyNumberVariableToEnd();

			//dataGridViewVariables.Focus();
		}

		private void updateDataGrid()
		{
			if (SelectedCommand == null)
				return;

			DataGridViewRow[] oldRows = new DataGridViewRow[dataGridViewVariables.Rows.Count];
			dataGridViewVariables.Rows.CopyTo(oldRows, 0);

			Dictionary<string, DataGridViewRow> oldVariableValueMap = new Dictionary<string, DataGridViewRow>();
			foreach (DataGridViewRow row in oldRows)
			{
				oldVariableValueMap[row.Cells[0].Value.ToString()] = row;
			}

			HashSet<string> variablesWithNumericSuffix = new HashSet<string>();

			foreach (string variable in SelectedCommand.GetAllVariables())
			{
				if (!oldVariableValueMap.ContainsKey(variable))
				{
					// works on windows (.NET) ------------------------------------
					//DataGridViewRow row = new DataGridViewRow();
					//string value = null;
					//command.VariableValueMap.TryGetValue(variable, out value);
					//row.CreateCells(dataGridViewVariables, variable, value);
					//dataGridViewVariables.Rows.Add(row);
					// workaround for mono: ----------------------------------------
					string value = null;
					SelectedCommand.VariableValueMap.TryGetValue(variable, out value);
					dataGridViewVariables.Rows.Add(variable, value);
					// -------------------------------------------------------------
				}

				string variableWithoutNumericSuffix;
				if (textHasNumericSuffix(variable, out variableWithoutNumericSuffix))
				{
				    DataGridViewRow rowToRemove;
				    if (oldVariableValueMap.TryGetValue(variableWithoutNumericSuffix, out rowToRemove) && dataGridViewVariables.Rows.Contains(rowToRemove)) // remove row without number
				    {
				        dataGridViewVariables.Rows.Remove(rowToRemove);
						SelectedCommand.VariableValueMap.Remove(variableWithoutNumericSuffix);
				    }
				    variablesWithNumericSuffix.Add(variable);
				}
			}

			foreach (string oldVar in oldVariableValueMap.Keys)
			{
				string variableWithoutNumericSuffix;
				if (textHasNumericSuffix(oldVar, out variableWithoutNumericSuffix) && !variablesWithNumericSuffix.Contains(oldVar))
				{
					DataGridViewRow rowToRemove;
					if (oldVariableValueMap.TryGetValue(oldVar, out rowToRemove) && dataGridViewVariables.Rows.Contains(rowToRemove)) // remove row with number not contained in new set of variables
					{
						dataGridViewVariables.Rows.Remove(rowToRemove);
						SelectedCommand.VariableValueMap.Remove(oldVar);
					}
				}
			}

			//movePropertyNumberVariableToEnd();
		}

		//private void movePropertyNumberVariableToEnd()
		//{
		//    DataGridViewRow propertyRow = null;
		//    foreach (DataGridViewRow row in dataGridViewVariables.Rows)
		//    {
		//        if ((row.Cells[0].Value ?? string.Empty).ToString() == PropertyCommand.PropertyNumberVariableName)
		//        {
		//            propertyRow = row;
		//            break;
		//        }
		//    }

		//    if (propertyRow != null)
		//    {
		//        dataGridViewVariables.Rows.Remove(propertyRow);
		//        dataGridViewVariables.Rows.Add(propertyRow);
		//    }
		//}

		private bool textHasNumericSuffix(string text, out string textWithoutSuffix)
		{
			StringBuilder prefix = new StringBuilder();
			foreach (char ch in text)
			{
				if (char.IsDigit(ch))
				{
					textWithoutSuffix = prefix.ToString();
					return true;
				}
				prefix.Append(ch);
			}
			textWithoutSuffix = text;
			return false;
		}

		private void dataGridViewVariables_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			if (initializing)
				return;

			string name = dataGridViewVariables.Rows[e.RowIndex].Cells[0].Value.ToString();
			Debug.Assert(e.ColumnIndex == 1);
			object value = dataGridViewVariables.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
			string valueString = (value != null) ? value.ToString() : null;
			if (!string.IsNullOrEmpty(valueString))
				SelectedCommand.VariableValueMap[name] = valueString;
			else
				SelectedCommand.VariableValueMap.Remove(name);

			labelFilledPattern.Text = SelectedCommand.FillPattern();
			labelResultText.Text = SelectedCommand.ToString();
			updateDataGrid();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel; // close dialog
		}

		private void buttonAddCommand_Click(object sender, EventArgs e)
		{
			try
			{
				initializing = true;

				Debug.Assert(comboBoxPropertyType.Items.Count > 0);

				PropertyCommand newCommand = new PropertyCommand(this.property, (PropertyCommand.CommandType)Enum.Parse(typeof(PropertyCommand.CommandType), comboBoxPropertyType.Items[0].ToString()));

				newCommand.VariableValueMap[PropertyCommand.PropertyNumberVariableName] = this.property.ToString();
				commands.Add(newCommand);
				addCommand(newCommand);
				SelectedCommand = newCommand;
				comboBoxPropertyType.SelectedItem = SelectedCommand.Type.ToString();
			}
			finally
			{
				initializing = false;
			}
		}

		private void buttonRemoveCommand_Click(object sender, EventArgs e)
		{
			if (commands.Count == 0 || SelectedCommand == null)
				return;
			try
			{
				initializing = true;

				int selectedIndex = commands.IndexOf(SelectedCommand);
				commands.Remove(SelectedCommand);
				comboBoxAllCommands.Items.RemoveAt(selectedIndex);

				SelectedCommand = (commands.Count > 0) ? commands[Math.Min(selectedIndex, commands.Count - 1)] : null;
			}
			finally
			{
				initializing = false;
			}
		}

		private void comboBoxAllCommands_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (initializing)
				return;
			try
			{
				initializing = true;
				SelectedCommand = (comboBoxAllCommands.SelectedIndex >= 0) ? commands[comboBoxAllCommands.SelectedIndex] : null;
			}
			finally
			{
				initializing = false;
			}
		}

		//private void dataGridViewVariables_KeyDown(object sender, KeyEventArgs e)
		//{
		//    if (e.KeyData == Keys.Enter)
		//    {
		//        dataGridViewVariables.EndEdit();
		//        e.Handled = true;
		//    }
		//}
	}
}
