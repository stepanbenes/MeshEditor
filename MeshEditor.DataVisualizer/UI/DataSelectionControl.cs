using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.LayerManager.Data;
using MeshEditor.DataVisualizer;

namespace MeshEditor.WinUI
{
	public partial class DataSelectionControl : UserControl
	{
		class ComboBoxItem<TKey, TValue>
		{
			public TKey Key { get; }
			public TValue Value { get; }
			public ComboBoxItem(TKey key, TValue value)
			{
				Key = key;
				Value = value;
			}
			public override string ToString()
			{
				return Key?.ToString() ?? "";
			}
		}

		SummaryFile layerSummary;
		bool updatingDataSource;

		//public int? GetMeshIndexOfCurrentDataSelection()
		//{
		//	return (comboBoxTimeStep.SelectedItem as ComboBoxItem<double, TimeStepDescriptor>)?.Value.MeshIndex;
		//}

		public DataSelection GetDataSelection()
		{
			var selectedFieldComboBoxItem = comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>;
			var selectedComponentComboBoxItem = comboBoxComponent.SelectedItem as ComboBoxItem<string, ComponentDescriptor>;
			var selectedTimeStepComboBoxItem = comboBoxTimeStep.SelectedItem as ComboBoxItem<double, TimeStepDescriptor>;

			if (selectedFieldComboBoxItem == null || selectedComponentComboBoxItem == null || selectedTimeStepComboBoxItem == null)
				return null;

			return new DataSelection(selectedFieldComboBoxItem.Key, selectedComponentComboBoxItem.Key, selectedTimeStepComboBoxItem.Key, selectedTimeStepComboBoxItem.Value.DataIndex, selectedTimeStepComboBoxItem.Value.MeshIndex);
		}

		//public void UpdateDataSelection(DataSelection dataSelection)
		//{
		//	if (dataSelection == null)
		//	{
		//		comboBoxField.SelectedItem = null;
		//		return;
		//	}

		//	comboBoxField.SelectedItem = comboBoxField.Items.Cast<ComboBoxItem<string, FieldDescriptor>>().SingleOrDefault(f => f.Key == dataSelection.FieldName);
		//	comboBoxComponent.SelectedItem = comboBoxComponent.Items.Cast<ComboBoxItem<string, ComponentDescriptor>>().SingleOrDefault(c => c.Key == dataSelection.ComponentName);
		//	comboBoxTimeStep.SelectedItem = comboBoxTimeStep.Items.Cast<ComboBoxItem<double, TimeStepDescriptor>>().SingleOrDefault(t => t.Key == dataSelection.TimeStep);
		//}

		public event EventHandler<DataSelectionEventArgs> DataSelectionChanged;

		public DataSelectionControl()
		{
			InitializeComponent();
		}

		public void UpdateDataSource(SummaryFile layerSummary, DataSelection dataSelection)
		{
			try
			{
				updatingDataSource = true;
				this.layerSummary = layerSummary;

				comboBoxTimeStep.Items.Clear();
				comboBoxComponent.Items.Clear();
				comboBoxField.Items.Clear();

				if (layerSummary != null)
				{
					comboBoxField.Items.AddRange(layerSummary.Fields.Select(pair => new ComboBoxItem<string, FieldDescriptor>(pair.Key, pair.Value)).ToArray());
					if (comboBoxField.Items.Count > 0)
					{
						ComboBoxItem<string, FieldDescriptor> itemToSelect = null;
						if (dataSelection != null)
						{
							itemToSelect = comboBoxField.Items.Cast<ComboBoxItem<string, FieldDescriptor>>().SingleOrDefault(f => f.Key == dataSelection.FieldName);
						}
						comboBoxField.SelectedItem = itemToSelect;
					}
					if (comboBoxComponent.Items.Count > 0)
					{
						ComboBoxItem<string, ComponentDescriptor> itemToSelect = null;
						if (dataSelection != null)
						{
							itemToSelect = comboBoxComponent.Items.Cast<ComboBoxItem<string, ComponentDescriptor>>().SingleOrDefault(c => c.Key == dataSelection.ComponentName);
						}
						comboBoxComponent.SelectedItem = itemToSelect;
					}
					if (comboBoxTimeStep.Items.Count > 0)
					{
						ComboBoxItem<double, TimeStepDescriptor> itemToSelect = null;
						if (dataSelection != null)
						{
							itemToSelect = comboBoxTimeStep.Items.Cast<ComboBoxItem<double, TimeStepDescriptor>>().SingleOrDefault(t => t.Key == dataSelection.TimeStep);
						}
						comboBoxTimeStep.SelectedItem = itemToSelect;
					}
				}
			}
			finally
			{
				updatingDataSource = false;
			}
		}

		private void comboBoxField_SelectedIndexChanged(object sender, EventArgs e)
		{
			comboBoxComponent.Items.Clear();
			var selectedFieldComboBoxItem = comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>;
			if (selectedFieldComboBoxItem != null)
			{
				comboBoxComponent.Items.AddRange(selectedFieldComboBoxItem.Value.Components.Select(pair => new ComboBoxItem<string, ComponentDescriptor>(pair.Key, pair.Value)).ToArray());
				if (!updatingDataSource && comboBoxComponent.Items.Count > 0)
					comboBoxComponent.SelectedIndex = 0;
			}
		}

		private void comboBoxComponent_SelectedIndexChanged(object sender, EventArgs e)
		{
			comboBoxTimeStep.Items.Clear();
			var selectedComponentComboBoxItem = comboBoxComponent.SelectedItem as ComboBoxItem<string, ComponentDescriptor>;
			if (selectedComponentComboBoxItem != null)
			{
				comboBoxTimeStep.Items.AddRange(selectedComponentComboBoxItem.Value.TimeSteps.Select(pair => new ComboBoxItem<double, TimeStepDescriptor>(pair.Key, pair.Value)).ToArray());
				if (!updatingDataSource && comboBoxTimeStep.Items.Count > 0)
					comboBoxTimeStep.SelectedIndex = 0;
			}
		}

		private void comboBoxTimeStep_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (updatingDataSource)
				return;

			DataSelectionChanged?.Invoke(this, new DataSelectionEventArgs(GetDataSelection()));
		}
	}
}
