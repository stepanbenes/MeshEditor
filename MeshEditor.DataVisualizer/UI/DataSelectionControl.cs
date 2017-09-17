using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class DataSelectionControl : UserControl
	{
		private class ComboBoxItem<TKey, TValue>
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

		#region Fields, constructor

		SummaryFile layerSummary;
		bool updatingDataSource;

		public DataSelectionControl()
		{
			InitializeComponent();
		}

		#endregion

		#region Public members

		public event EventHandler<DataSelectionEventArgs> DataSelectionChanged;

		public void UpdateDataSource(SummaryFile layerSummary, DataSelection dataSelection)
		{
			try
			{
				updatingDataSource = true;
				this.layerSummary = layerSummary;

				comboBoxTimeStep.Items.Clear();
				comboBoxField.Items.Clear();
				comboBoxComponent.Items.Clear();

				if (layerSummary != null)
				{
					setupTimeSteps();

					if (dataSelection != null)
					{
						setupFields(dataSelection.TimeStep);
						setupComponents(dataSelection.FieldName);

						comboBoxTimeStep.SelectedItem = comboBoxTimeStep.Items.Cast<ComboBoxItem<double, IMeshFileDescriptor>>().SingleOrDefault(item => item.Key == dataSelection.TimeStep);
						comboBoxField.SelectedItem = comboBoxField.Items.Cast<ComboBoxItem<string, FieldDescriptor>>().SingleOrDefault(item => item.Key == dataSelection.FieldName);
						comboBoxComponent.SelectedItem = comboBoxComponent.Items.Cast<ComboBoxItem<string, ComponentDescriptor>>().SingleOrDefault(item => item.Key == dataSelection.ComponentName);
					}
				}
			}
			finally
			{
				updatingDataSource = false;
			}
		}

		#endregion

		#region Private methods

		private DataSelection getDataSelection()
		{
			var selectedTimeStepComboBoxItem = comboBoxTimeStep.SelectedItem as ComboBoxItem<double, IMeshFileDescriptor>;
			if (selectedTimeStepComboBoxItem == null)
				return null;

			var selectedFieldComboBoxItem = comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>;
			var selectedComponentComboBoxItem = comboBoxComponent.SelectedItem as ComboBoxItem<string, ComponentDescriptor>;

			double timeStep = selectedTimeStepComboBoxItem.Key;
			IMeshFileDescriptor mesh = selectedTimeStepComboBoxItem.Value;
			string fieldName = selectedFieldComboBoxItem?.Key;
			string componentName = selectedComponentComboBoxItem?.Key;
			TimeStepDescriptor timeStepDescriptor = selectedComponentComboBoxItem?.Value.TimeSteps.SingleOrDefault(timeStepPair => timeStepPair.Key == timeStep).Value;
			int? dataIndex = timeStepDescriptor?.DataIndex;

			return new DataSelection(fieldName, componentName, timeStep, dataIndex, null /**/, mesh);
		}

		private void comboBoxTimeStep_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingDataSource)
			{
				try
				{
					updatingDataSource = true;
					string selectedFieldName = (comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>)?.Key;
					string selectedComponentName = (comboBoxComponent.SelectedItem as ComboBoxItem<string, ComponentDescriptor>)?.Key;

					setupFields(selectedTimeStep: (comboBoxTimeStep.SelectedItem as ComboBoxItem<double, IMeshFileDescriptor>)?.Key);

					comboBoxField.SelectedItem = comboBoxField.Items.Cast<ComboBoxItem<string, FieldDescriptor>>().SingleOrDefault(item => item.Key == selectedFieldName);
					comboBoxComponent.SelectedItem = comboBoxComponent.Items.Cast<ComboBoxItem<string, ComponentDescriptor>>().SingleOrDefault(item => item.Key == selectedComponentName);
				}
				finally
				{
					updatingDataSource = false;
				}
				notifyDataSelectionChanged();
			}
		}

		private void comboBoxField_SelectedIndexChanged(object sender, EventArgs e)
		{
			setupComponents(selectedFieldName: (comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>)?.Key);
			// select first component
			if (comboBoxComponent.Items.Count > 0)
			{
				comboBoxComponent.SelectedIndex = 0;
			}
		}

		private void comboBoxComponent_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingDataSource)
			{
				notifyDataSelectionChanged();
			}
		}

		private void setupTimeSteps()
		{
			Debug.Assert(layerSummary != null);
			comboBoxTimeStep.Items.Clear();
			comboBoxTimeStep.Items.AddRange(layerSummary.Meshes.SelectMany(mesh => mesh.TimeSteps.Select(timeStep => new ComboBoxItem<double, IMeshFileDescriptor>(timeStep, mesh))).ToArray());
		}

		private void setupFields(double? selectedTimeStep)
		{
			Debug.Assert(layerSummary != null);
			comboBoxField.Items.Clear();
			if (selectedTimeStep.HasValue)
			{
				comboBoxField.Items.AddRange(getAvailableFields(selectedTimeStep.Value).Select(pair => new ComboBoxItem<string, FieldDescriptor>(pair.fieldName, pair.fieldDescriptor)).ToArray());
			}
		}

		private void setupComponents(string selectedFieldName)
		{
			Debug.Assert(layerSummary != null);
			comboBoxComponent.Items.Clear();
			if (selectedFieldName != null && layerSummary.Fields.TryGetValue(selectedFieldName, out FieldDescriptor selectedField))
			{
				comboBoxComponent.Items.AddRange(selectedField.Components.Select(pair => new ComboBoxItem<string, ComponentDescriptor>(pair.Key, pair.Value)).ToArray());
			}
		}

		private void notifyDataSelectionChanged()
		{
			Debug.Assert(layerSummary != null);
			DataSelectionChanged?.Invoke(this, new DataSelectionEventArgs(layerSummary.Id, layerSummary.Name, getDataSelection()));
		}

		private IEnumerable<(string fieldName, FieldDescriptor fieldDescriptor)> getAvailableFields(double selectedTimeStep)
		{
			Debug.Assert(layerSummary != null);

			return from fieldPair in layerSummary.Fields
				   where (from componentPair in fieldPair.Value.Components
						  from timeStepPair in componentPair.Value.TimeSteps
						  select timeStepPair.Key).Any(timeStep => timeStep == selectedTimeStep)
				   select (fieldPair.Key, fieldPair.Value);
		}

		#endregion
	}
}
