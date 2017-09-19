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

		private static readonly string NoneItem = "[None]";

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
				comboBoxVectorField.Items.Clear();

				if (layerSummary != null)
				{
					setupTimeSteps();

					if (dataSelection != null)
					{
						setupFields(dataSelection.TimeStep);
						setupComponents(dataSelection.FieldName);
						setupVectorFields(dataSelection.TimeStep);

						comboBoxTimeStep.SelectedItem = comboBoxTimeStep.Items.OfType<ComboBoxItem<decimal, IMeshFileDescriptor>>().SingleOrDefault(item => item.Key == dataSelection.TimeStep);
						comboBoxField.SelectedItem = comboBoxField.Items.OfType<ComboBoxItem<string, FieldDescriptor>>().SingleOrDefault(item => item.Key == dataSelection.FieldName) ?? (object)NoneItem;
						comboBoxComponent.SelectedItem = comboBoxComponent.Items.OfType<ComboBoxItem<string, ComponentDescriptor>>().SingleOrDefault(item => item.Key == dataSelection.ComponentName);
						comboBoxVectorField.SelectedItem = comboBoxVectorField.Items.OfType<ComboBoxItem<string, FieldDescriptor>>().SingleOrDefault(item => item.Key == dataSelection.VectorFieldName) ?? (object)NoneItem;
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

		#region Event handlers

		private void comboBoxTimeStep_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingDataSource)
			{
				try
				{
					updatingDataSource = true;
					string selectedFieldName = (comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>)?.Key;
					string selectedComponentName = (comboBoxComponent.SelectedItem as ComboBoxItem<string, ComponentDescriptor>)?.Key;
					decimal? selectedTimeStep = (comboBoxTimeStep.SelectedItem as ComboBoxItem<decimal, IMeshFileDescriptor>)?.Key;
					string selectedVectorFieldName = (comboBoxVectorField.SelectedItem as ComboBoxItem<string, FieldDescriptor>)?.Key;

					setupFields(selectedTimeStep);
					setupVectorFields(selectedTimeStep);

					comboBoxField.SelectedItem = comboBoxField.Items.OfType<ComboBoxItem<string, FieldDescriptor>>().SingleOrDefault(item => item.Key == selectedFieldName) ?? (object)NoneItem;
					comboBoxComponent.SelectedItem = comboBoxComponent.Items.OfType<ComboBoxItem<string, ComponentDescriptor>>().SingleOrDefault(item => item.Key == selectedComponentName);
					comboBoxVectorField.SelectedItem = comboBoxVectorField.Items.OfType<ComboBoxItem<string, FieldDescriptor>>().SingleOrDefault(item => item.Key == selectedVectorFieldName) ?? (object)NoneItem;
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
				comboBoxComponent.Enabled = true;
				comboBoxComponent.SelectedIndex = 0;
			}
			else
			{
				comboBoxComponent.Enabled = false;
				if (!updatingDataSource)
				{
					notifyDataSelectionChanged();
				}
			}
		}

		private void comboBoxComponent_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingDataSource)
			{
				notifyDataSelectionChanged();
			}
		}

		private void comboBoxVectorField_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingDataSource)
			{
				notifyDataSelectionChanged();
			}
		}

		#endregion

		private DataSelection getDataSelection()
		{
			var selectedTimeStepComboBoxItem = comboBoxTimeStep.SelectedItem as ComboBoxItem<decimal, IMeshFileDescriptor>;
			if (selectedTimeStepComboBoxItem == null)
				return null;

			var selectedFieldComboBoxItem = comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>;
			var selectedComponentComboBoxItem = comboBoxComponent.SelectedItem as ComboBoxItem<string, ComponentDescriptor>;
			var selectedVectorFieldComboBoxItem = comboBoxVectorField.SelectedItem as ComboBoxItem<string, FieldDescriptor>;

			decimal timeStep = selectedTimeStepComboBoxItem.Key;
			IMeshFileDescriptor mesh = selectedTimeStepComboBoxItem.Value;
			string fieldName = selectedFieldComboBoxItem?.Key;
			string componentName = selectedComponentComboBoxItem?.Key;
			TimeStepDescriptor timeStepDescriptor = selectedComponentComboBoxItem?.Value.TimeSteps.SingleOrDefault(timeStepPair => timeStepPair.Key == timeStep).Value;
			int? dataIndex = timeStepDescriptor?.DataIndex;
			string vectorFieldName = selectedVectorFieldComboBoxItem?.Key;

			var vectorTimeStepDescriptors = selectedVectorFieldComboBoxItem?.Value.Components.OrderBy(componentPair => componentPair.Key).Select(componentPair => componentPair.Value).SelectMany(c => c.TimeSteps).Where(timeStepPair => timeStepPair.Key == timeStep).Select(timeStepPair => timeStepPair.Value).ToList();
			Debug.Assert(vectorTimeStepDescriptors == null || vectorTimeStepDescriptors.Count == 3);
			VectorIndex? vectorIndex = vectorTimeStepDescriptors != null ? new VectorIndex(vectorTimeStepDescriptors[0].DataIndex, vectorTimeStepDescriptors[1].DataIndex, vectorTimeStepDescriptors[2].DataIndex) : (VectorIndex?)null;

			return new DataSelection(fieldName, componentName, timeStep, dataIndex, vectorFieldName, vectorIndex, mesh);
		}

		private void setupTimeSteps()
		{
			Debug.Assert(layerSummary != null);
			comboBoxTimeStep.Items.Clear();
			comboBoxTimeStep.Items.AddRange(layerSummary.Meshes.SelectMany(mesh => mesh.TimeSteps.Select(timeStep => new ComboBoxItem<decimal, IMeshFileDescriptor>(timeStep, mesh))).ToArray());
		}

		private void setupFields(decimal? selectedTimeStep)
		{
			Debug.Assert(layerSummary != null);
			comboBoxField.Items.Clear();
			if (selectedTimeStep.HasValue)
			{
				comboBoxField.Items.Add(NoneItem);
				comboBoxField.Items.AddRange(getAvailableFields(selectedTimeStep.Value).Select(pair => new ComboBoxItem<string, FieldDescriptor>(pair.fieldName, pair.fieldDescriptor)).ToArray());
			}
		}

		private void setupVectorFields(decimal? selectedTimeStep)
		{
			Debug.Assert(layerSummary != null);
			comboBoxVectorField.Items.Clear();
			if (selectedTimeStep.HasValue)
			{
				comboBoxVectorField.Items.Add(NoneItem);
				comboBoxVectorField.Items.AddRange(getAvailableVectorFields(selectedTimeStep.Value).Select(pair => new ComboBoxItem<string, FieldDescriptor>(pair.fieldName, pair.fieldDescriptor)).ToArray());
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

		private IEnumerable<(string fieldName, FieldDescriptor fieldDescriptor)> getAvailableFields(decimal selectedTimeStep)
		{
			Debug.Assert(layerSummary != null);

			return from fieldPair in layerSummary.Fields
				   where (from componentPair in fieldPair.Value.Components
						  from timeStepPair in componentPair.Value.TimeSteps
						  select timeStepPair.Key).Any(timeStep => timeStep == selectedTimeStep)
				   select (fieldPair.Key, fieldPair.Value);
		}

		private IEnumerable<(string fieldName, FieldDescriptor fieldDescriptor)> getAvailableVectorFields(decimal selectedTimeStep)
		{
			return getAvailableFields(selectedTimeStep).Where(field => field.fieldDescriptor.Components.Count == 3); // TODO: is this condition enough?
		}

		#endregion
	}
}
