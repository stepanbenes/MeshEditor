using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MeshEditor.DataVisualizer;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// Selection of time step, scalar field / component and vector field of the selected layer
	/// (port of MeshEditor.DataVisualizer.UI.DataSelectionControl).
	/// </summary>
	public class DataSelectionControl : StackPanel
	{
		private sealed class ComboBoxItem<TKey, TValue>
		{
			public TKey Key { get; }
			public TValue Value { get; }

			public ComboBoxItem(TKey key, TValue value)
			{
				Key = key;
				Value = value;
			}

			public override string ToString() => Key?.ToString() ?? "";
		}

		private static readonly string NoneItem = "[None]";

		private SummaryFile layerSummary;
		private bool updatingDataSource;
		private IVisualizerSettings visualizerSettings;

		internal readonly ComboBox comboBoxTimeStep, comboBoxField, comboBoxComponent, comboBoxVectorField;
		internal readonly HyperlinkButton linkLabelEditColorScale, linkLabelVectorFieldSettings;

		public event EventHandler<DataSelectionEventArgs> DataSelectionChanged;
		public event EventHandler VisualizerSettingsChanged;

		public DataSelectionControl()
		{
			Spacing = 3;
			ComboBox combo() => new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 26 };
			TextBlock label(string text) => new TextBlock { Text = text, Margin = new Thickness(0, 6, 0, 0) };
			HyperlinkButton link(string text) => new HyperlinkButton { Content = text, Padding = new Thickness(0), IsEnabled = false };

			comboBoxTimeStep = combo();
			comboBoxField = combo();
			comboBoxComponent = combo();
			comboBoxVectorField = combo();
			linkLabelEditColorScale = link("Color scale settings");
			linkLabelVectorFieldSettings = link("Vector field visualization settings");

			Children.Add(new TextBlock { Text = "Data selection", FontWeight = FontWeight.Bold, Margin = new Thickness(2, 0, 0, 2) });
			Children.Add(label("Time step"));
			Children.Add(comboBoxTimeStep);
			Children.Add(label("Scalars"));
			Children.Add(comboBoxField);
			Children.Add(comboBoxComponent);
			Children.Add(linkLabelEditColorScale);
			Children.Add(label("Vectors"));
			Children.Add(comboBoxVectorField);
			Children.Add(linkLabelVectorFieldSettings);

			comboBoxTimeStep.SelectionChanged += comboBoxTimeStep_SelectedIndexChanged;
			comboBoxField.SelectionChanged += comboBoxField_SelectedIndexChanged;
			comboBoxComponent.SelectionChanged += comboBoxComponent_SelectedIndexChanged;
			comboBoxVectorField.SelectionChanged += comboBoxVectorField_SelectedIndexChanged;
			linkLabelEditColorScale.Click += linkLabelEditColorScale_LinkClicked;
			linkLabelVectorFieldSettings.Click += linkLabelVectorFieldSettings_LinkClicked;
		}

		/// <summary>
		/// Current selection (for tests).
		/// </summary>
		internal DataSelection CurrentDataSelection => getDataSelection();

		public void UpdateDataSource(SummaryFile layerSummary, DataSelection dataSelection, IVisualizerSettings visualizerSettings)
		{
			try
			{
				updatingDataSource = true;

				this.layerSummary = layerSummary;
				this.visualizerSettings = visualizerSettings;

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

				linkLabelEditColorScale.IsEnabled = (visualizerSettings != null && dataSelection?.HasScalarSelection == true);
				linkLabelVectorFieldSettings.IsEnabled = (visualizerSettings != null && dataSelection?.HasVectorSelection == true);
			}
			finally
			{
				updatingDataSource = false;
			}
		}

		#region Event handlers

		private void comboBoxTimeStep_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
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

		private void comboBoxField_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (layerSummary == null)
				return;
			setupComponents(selectedFieldName: (comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>)?.Key);
			if (comboBoxComponent.Items.Count > 0)
			{
				comboBoxComponent.IsEnabled = true;
				comboBoxComponent.SelectedIndex = 0;
			}
			else
			{
				comboBoxComponent.IsEnabled = false;
				if (!updatingDataSource)
					notifyDataSelectionChanged();
			}
		}

		private void comboBoxComponent_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!updatingDataSource && comboBoxComponent.SelectedItem != null)
				notifyDataSelectionChanged();
		}

		private void comboBoxVectorField_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!updatingDataSource)
				notifyDataSelectionChanged();
		}

		private async void linkLabelEditColorScale_LinkClicked(object sender, RoutedEventArgs ea)
		{
			Debug.Assert(visualizerSettings != null);
			var editColorScaleForm = new ColorScaleSettingsDialog(visualizerSettings);
			editColorScaleForm.SettingsChanged += (s, e) => VisualizerSettingsChanged?.Invoke(s, e);
			await editColorScaleForm.ShowDialog(TopLevel.GetTopLevel(this) as Window);
		}

		private async void linkLabelVectorFieldSettings_LinkClicked(object sender, RoutedEventArgs ea)
		{
			Debug.Assert(visualizerSettings != null);
			var vectorFieldVisualizationSettingsForm = new VectorFieldVisualizationSettingsDialog(visualizerSettings);
			vectorFieldVisualizationSettingsForm.SettingsChanged += (s, e) => VisualizerSettingsChanged?.Invoke(s, e);
			await vectorFieldVisualizationSettingsForm.ShowDialog(TopLevel.GetTopLevel(this) as Window);
		}

		#endregion

		private DataSelection getDataSelection()
		{
			if (comboBoxTimeStep.SelectedItem is not ComboBoxItem<decimal, IMeshFileDescriptor> selectedTimeStepComboBoxItem)
				return null;
			var selectedFieldComboBoxItem = comboBoxField.SelectedItem as ComboBoxItem<string, FieldDescriptor>;
			var selectedComponentComboBoxItem = comboBoxComponent.SelectedItem as ComboBoxItem<string, ComponentDescriptor>;
			var selectedVectorFieldComboBoxItem = comboBoxVectorField.SelectedItem as ComboBoxItem<string, FieldDescriptor>;
			return new DataSelection(selectedTimeStepComboBoxItem.Key, selectedFieldComboBoxItem?.Key, selectedComponentComboBoxItem?.Key, selectedVectorFieldComboBoxItem?.Key, selectedTimeStepComboBoxItem.Value);
		}

		private void setupTimeSteps()
		{
			Debug.Assert(layerSummary != null);
			comboBoxTimeStep.Items.Clear();
			foreach (var item in layerSummary.Meshes.SelectMany(mesh => mesh.TimeSteps.Select(timeStep => new ComboBoxItem<decimal, IMeshFileDescriptor>(timeStep, mesh))))
				comboBoxTimeStep.Items.Add(item);
		}

		private void setupFields(decimal? selectedTimeStep)
		{
			Debug.Assert(layerSummary != null);
			comboBoxField.Items.Clear();
			if (selectedTimeStep.HasValue)
			{
				comboBoxField.Items.Add(NoneItem);
				foreach (var pair in getAvailableFields(selectedTimeStep.Value))
					comboBoxField.Items.Add(new ComboBoxItem<string, FieldDescriptor>(pair.fieldName, pair.fieldDescriptor));
			}
		}

		private void setupVectorFields(decimal? selectedTimeStep)
		{
			Debug.Assert(layerSummary != null);
			comboBoxVectorField.Items.Clear();
			if (selectedTimeStep.HasValue)
			{
				comboBoxVectorField.Items.Add(NoneItem);
				foreach (var pair in getAvailableVectorFields(selectedTimeStep.Value))
					comboBoxVectorField.Items.Add(new ComboBoxItem<string, FieldDescriptor>(pair.fieldName, pair.fieldDescriptor));
			}
		}

		private void setupComponents(string selectedFieldName)
		{
			Debug.Assert(layerSummary != null);
			comboBoxComponent.Items.Clear();
			if (selectedFieldName != null && layerSummary.Fields.TryGetValue(selectedFieldName, out FieldDescriptor selectedField))
			{
				foreach (var pair in selectedField.Components)
					comboBoxComponent.Items.Add(new ComboBoxItem<string, ComponentDescriptor>(pair.Key, pair.Value));
			}
		}

		private void notifyDataSelectionChanged()
		{
			Debug.Assert(layerSummary != null);
			var dataSelection = getDataSelection();
			if (dataSelection == null)
				return;
			linkLabelEditColorScale.IsEnabled = dataSelection.HasScalarSelection;
			linkLabelVectorFieldSettings.IsEnabled = dataSelection.HasVectorSelection;
			DataSelectionChanged?.Invoke(this, new DataSelectionEventArgs(layerSummary.Id, layerSummary.Name, dataSelection));
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
			return getAvailableFields(selectedTimeStep).Where(field => field.fieldDescriptor.Components.Count <= 3);
		}
	}
}
