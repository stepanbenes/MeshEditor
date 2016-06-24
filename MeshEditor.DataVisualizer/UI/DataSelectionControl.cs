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

		public int? SelectedMeshIndex => (comboBoxTimeStep.SelectedItem as ComboBoxItem<double, TimeStepDescriptor>)?.Value.MeshIndex;
		public int? SelectedDataIndex => (comboBoxTimeStep.SelectedItem as ComboBoxItem<double, TimeStepDescriptor>)?.Value.DataIndex;

		public event EventHandler<DataSelectionEventArgs> DataSelectionChanged;

		public DataSelectionControl()
		{
			InitializeComponent();
		}

		public void UpdateDataSource(SummaryFile layerSummary)
		{
			this.layerSummary = layerSummary;

			comboBoxTimeStep.Items.Clear();
			comboBoxComponent.Items.Clear();
			comboBoxField.Items.Clear();

			comboBoxField.Items.AddRange(layerSummary.Fields.Select(pair => new ComboBoxItem<string, FieldDescriptor>(pair.Key, pair.Value)).ToArray());
			if (comboBoxField.Items.Count > 0)
				comboBoxField.SelectedIndex = 0;
		}

		private void comboBoxField_SelectedIndexChanged(object sender, EventArgs e)
		{
			comboBoxComponent.Items.Clear();
			var selectedField = comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>;
			if (selectedField != null)
			{
				comboBoxComponent.Items.AddRange(selectedField.Value.Components.Select(pair => new ComboBoxItem<string, ComponentDescriptor>(pair.Key, pair.Value)).ToArray());
				if (comboBoxComponent.Items.Count > 0)
					comboBoxComponent.SelectedIndex = 0;
			}
		}

		private void comboBoxComponent_SelectedIndexChanged(object sender, EventArgs e)
		{
			comboBoxTimeStep.Items.Clear();
			var selectedComponent = comboBoxComponent.SelectedItem as ComboBoxItem<string, ComponentDescriptor>;
			if (selectedComponent != null)
			{
				comboBoxTimeStep.Items.AddRange(selectedComponent.Value.TimeSteps.Select(pair => new ComboBoxItem<double, TimeStepDescriptor>(pair.Key, pair.Value)).ToArray());
				if (comboBoxTimeStep.Items.Count > 0)
					comboBoxTimeStep.SelectedIndex = 0;
			}
		}

		private void comboBoxTimeStep_SelectedIndexChanged(object sender, EventArgs e)
		{
			var selectedTimeStep = comboBoxTimeStep.SelectedItem as ComboBoxItem<double, TimeStepDescriptor>;
			if (selectedTimeStep != null)
			{
				DataSelectionChanged?.Invoke(this, new DataSelectionEventArgs(selectedTimeStep.Value.MeshIndex, selectedTimeStep.Value.DataIndex, selectedTimeStep.Key));
			}
		}
	}
}
