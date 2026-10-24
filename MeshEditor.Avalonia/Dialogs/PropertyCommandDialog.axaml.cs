using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MeshEditor.Data;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Editor of the preprocessor commands of a property (port of MeshEditor.WinUI.PropertyCommandForm).
	/// </summary>
	public partial class PropertyCommandDialog : Window
	{
		/// <summary>
		/// Row of the variable grid (DataGridViewRow in the WinForms form).
		/// </summary>
		internal sealed class VariableRow
		{
			public string Name { get; init; }
			public TextBox ValueBox { get; init; }
			public Control Container { get; init; }
			public bool ReadOnly => ValueBox.IsReadOnly;
		}

		private List<PropertyCommand> commands;
		private PropertyCommand selectedCommand;
		private readonly Property property;
		private readonly EntityType propertyTarget;
		private bool initializing;
		private bool dialogResultOK;
		private readonly List<VariableRow> variableRows = new List<VariableRow>();
		private readonly List<string> commandNames = new List<string>();

		public PropertyCommandDialog()
			: this(new PropertyEntityPair(new Property(0), EntityType.Region), null)
		{ }

		public PropertyCommandDialog(PropertyEntityPair propertyTargetPair, List<PropertyCommand> savedCommands)
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

		public List<PropertyCommand> Commands => commands;

		internal IReadOnlyList<VariableRow> VariableRows => variableRows;

		public PropertyCommand SelectedCommand
		{
			get => selectedCommand;
			set
			{
				selectedCommand = value;
				onSelectedCommandChanged();
			}
		}

		/// <summary>
		/// Shows the dialog modally; true when confirmed by OK (DialogResult.OK).
		/// </summary>
		public new async Task<bool> ShowDialog(Window owner)
		{
			await this.ShowModalAsync(owner);
			return dialogResultOK;
		}

		private void onSelectedCommandChanged()
		{
			comboBoxAllCommands.SelectedIndex = commands.IndexOf(selectedCommand);
			bool enabled = selectedCommand != null;
			buttonRemoveCommand.IsEnabled = enabled;
			comboBoxAllCommands.IsEnabled = enabled;
			groupBoxCommandDescription.IsEnabled = enabled;

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
					comboBoxPropertyType.SelectedItem = SelectedCommand.Type.ToString();
			}
			finally
			{
				initializing = false;
			}
		}

		private void fillComboBoxPropertyType()
		{
			comboBoxPropertyType.ItemsSource = Enum.GetValues(typeof(PropertyCommand.CommandType)).Cast<PropertyCommand.CommandType>().Select(t => t.ToString()).ToList();
		}

		private void refreshCommandNames()
		{
			int selectedIndex = comboBoxAllCommands.SelectedIndex;
			comboBoxAllCommands.ItemsSource = commandNames.ToList();
			comboBoxAllCommands.SelectedIndex = selectedIndex;
		}

		private void updateNameOfCommand(PropertyCommand command)
		{
			int index = commands.IndexOf(command);
			if (index >= 0)
			{
				string commandName = "Command " + command.Type;
				string updatedName = commandName;
				int namesakeCount = 1;
				for (int i = 0; i < commandNames.Count; i++)
				{
					string name = commandNames[i];
					if (name.StartsWith(updatedName))
					{
						namesakeCount++;
						updatedName = commandName + " " + namesakeCount;
						i = 0; // start from beginning
					}
				}
				commandNames[index] = updatedName;
				refreshCommandNames();
			}
		}

		private void addCommand(PropertyCommand command)
		{
			Debug.Assert(command != null);
			commandNames.Add(string.Empty);
			updateNameOfCommand(command);
		}

		private void buttonOK_Click(object sender, RoutedEventArgs e)
		{
			commitFocusedValue();
			dialogResultOK = true;
			Close();
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void comboBoxPropertyType_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (initializing || comboBoxPropertyType.SelectedItem == null || SelectedCommand == null)
				return;

			SelectedCommand.Type = (PropertyCommand.CommandType)Enum.Parse(typeof(PropertyCommand.CommandType), comboBoxPropertyType.SelectedItem.ToString(), /*ignoreCase: */ true);
			updateNameOfCommand(SelectedCommand);
			SelectedCommand.VariableValueMap.Clear();
			SelectedCommand.VariableValueMap[PropertyCommand.PropertyNumberVariableName] = this.property.ToString();
			labelCommandPattern.Text = SelectedCommand.GetCommandPattern();
			labelFilledPattern.Text = SelectedCommand.FillPattern();
			labelResultText.Text = SelectedCommand.ToString();
			setupDataGrid();
		}

		#region Variable grid

		private VariableRow createRow(string variable, string value, bool readOnly)
		{
			var nameBlock = new TextBlock { Text = variable, Margin = new Thickness(4, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
			var valueBox = new TextBox { Text = value, IsReadOnly = readOnly, MinHeight = 24, Padding = new Thickness(4, 2), BorderThickness = new Thickness(0) };
			var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("130,*") };
			grid.Children.Add(nameBlock);
			Grid.SetColumn(valueBox, 1);
			grid.Children.Add(valueBox);
			var container = new Border { BorderBrush = Avalonia.Media.Brush.Parse("#E0E0E0"), BorderThickness = new Thickness(0, 0, 0, 1), Child = grid };
			var row = new VariableRow { Name = variable, ValueBox = valueBox, Container = container };
			if (!readOnly)
			{
				string lastCommitted = value;
				void commit()
				{
					if (valueBox.Text != lastCommitted && variableRows.Contains(row))
					{
						lastCommitted = valueBox.Text;
						dataGridViewVariables_CellEndEdit(row);
					}
				}
				valueBox.LostFocus += (s, e) => commit();
				valueBox.KeyDown += (s, e) =>
				{
					if (e.Key == Key.Enter)
					{
						commit();
						e.Handled = true;
					}
				};
			}
			return row;
		}

		private void addRow(string variable, string value, bool readOnly = false)
		{
			var row = createRow(variable, value, readOnly);
			variableRows.Add(row);
			dataGridViewVariables.Children.Add(row.Container);
		}

		private void removeRow(VariableRow row)
		{
			variableRows.Remove(row);
			dataGridViewVariables.Children.Remove(row.Container);
		}

		private void commitFocusedValue()
		{
			// an edit in progress ends when the dialog is confirmed (like DataGridView.EndEdit)
			var focused = variableRows.FirstOrDefault(r => r.ValueBox.IsFocused);
			if (focused != null)
				dataGridViewVariables_CellEndEdit(focused);
		}

		private void setupDataGrid()
		{
			foreach (var row in variableRows.ToList())
				removeRow(row);
			if (SelectedCommand == null)
				return;
			foreach (string variable in SelectedCommand.GetAllVariables())
			{
				SelectedCommand.VariableValueMap.TryGetValue(variable, out string value);
				addRow(variable, value, readOnly: variable == PropertyCommand.PropertyNumberVariableName);
			}
			// focus the first editable value
			variableRows.FirstOrDefault(r => !r.ReadOnly)?.ValueBox.Focus();
		}

		private void updateDataGrid()
		{
			if (SelectedCommand == null)
				return;

			var oldVariableValueMap = variableRows.ToDictionary(r => r.Name);

			HashSet<string> variablesWithNumericSuffix = new HashSet<string>();
			foreach (string variable in SelectedCommand.GetAllVariables())
			{
				if (!oldVariableValueMap.ContainsKey(variable))
				{
					SelectedCommand.VariableValueMap.TryGetValue(variable, out string value);
					addRow(variable, value);
				}
				if (textHasNumericSuffix(variable, out string variableWithoutNumericSuffix))
				{
					if (oldVariableValueMap.TryGetValue(variableWithoutNumericSuffix, out VariableRow rowToRemove) && variableRows.Contains(rowToRemove)) // remove row without number
					{
						removeRow(rowToRemove);
						SelectedCommand.VariableValueMap.Remove(variableWithoutNumericSuffix);
					}
					variablesWithNumericSuffix.Add(variable);
				}
			}

			foreach (string oldVar in oldVariableValueMap.Keys)
			{
				if (textHasNumericSuffix(oldVar, out _) && !variablesWithNumericSuffix.Contains(oldVar))
				{
					if (oldVariableValueMap.TryGetValue(oldVar, out VariableRow rowToRemove) && variableRows.Contains(rowToRemove)) // remove row with number not contained in new set of variables
					{
						removeRow(rowToRemove);
						SelectedCommand.VariableValueMap.Remove(oldVar);
					}
				}
			}
		}

		private static bool textHasNumericSuffix(string text, out string textWithoutSuffix)
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

		/// <summary>
		/// Value of a variable was edited (DataGridView.CellEndEdit).
		/// </summary>
		internal void dataGridViewVariables_CellEndEdit(VariableRow row)
		{
			if (initializing || SelectedCommand == null)
				return;
			string valueString = row.ValueBox.Text;
			if (!string.IsNullOrEmpty(valueString))
				SelectedCommand.VariableValueMap[row.Name] = valueString;
			else
				SelectedCommand.VariableValueMap.Remove(row.Name);
			labelFilledPattern.Text = SelectedCommand.FillPattern();
			labelResultText.Text = SelectedCommand.ToString();
			updateDataGrid();
		}

		#endregion

		private void buttonAddCommand_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				initializing = true;
				var types = (IList<string>)comboBoxPropertyType.ItemsSource;
				Debug.Assert(types.Count > 0);
				PropertyCommand newCommand = new PropertyCommand(this.property, (PropertyCommand.CommandType)Enum.Parse(typeof(PropertyCommand.CommandType), types[0]));
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

		private void buttonRemoveCommand_Click(object sender, RoutedEventArgs e)
		{
			if (commands.Count == 0 || SelectedCommand == null)
				return;
			try
			{
				initializing = true;
				int selectedIndex = commands.IndexOf(SelectedCommand);
				commands.Remove(SelectedCommand);
				commandNames.RemoveAt(selectedIndex);
				comboBoxAllCommands.SelectedIndex = -1;
				refreshCommandNames();
				SelectedCommand = (commands.Count > 0) ? commands[Math.Min(selectedIndex, commands.Count - 1)] : null;
			}
			finally
			{
				initializing = false;
			}
		}

		private void comboBoxAllCommands_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
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
	}
}
