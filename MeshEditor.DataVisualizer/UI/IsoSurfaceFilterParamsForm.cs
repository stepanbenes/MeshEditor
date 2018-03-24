using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class IsoSurfaceFilterParamsForm : FilterParamsForm
	{
		private readonly IReadOnlyDictionary<string, IEnumerable<string>> availableFieldComponents;

		public IsoSurfaceFilterParamsForm(IReadOnlyDictionary<string, IEnumerable<string>> availableFieldComponents)
		{
			Debug.Assert(availableFieldComponents != null);
			InitializeComponent();
			this.availableFieldComponents = availableFieldComponents;

			setupFields();

			textBoxValue.Text = 0.0.ToString(CultureInfo.InvariantCulture);

			updateLayerName();
		}

		private void setupFields()
		{
			comboBoxField.Items.Clear();
			comboBoxField.Items.AddRange(availableFieldComponents.Keys.ToArray());
			if (comboBoxField.Items.Count > 0)
			{
				comboBoxField.SelectedIndex = 0;
			}
		}

		private void setupComponents()
		{
			comboBoxComponent.Items.Clear();
			if (comboBoxField.SelectedItem is string selectedField && availableFieldComponents.TryGetValue(selectedField, out var components))
			{
				comboBoxComponent.Items.AddRange(components.ToArray());
				if (comboBoxComponent.Items.Count > 0)
				{
					comboBoxComponent.SelectedIndex = 0;
				}
			}
		}

		private void updateLayerName()
		{
			double value;
			if (!double.TryParse(textBoxValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				value = 0.0;
			}
			textBoxLayerName.Text = $"isosurface {comboBoxField.SelectedItem as string}/{comboBoxComponent.SelectedItem as string} = {value.ToString(CultureInfo.InvariantCulture)}";
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			string selectedField = comboBoxField.SelectedItem as string;
			string selectedComponent = comboBoxComponent.SelectedItem as string;
			string layerNameText = textBoxLayerName.Text;
			double value;

			if (!double.TryParse(textBoxValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				value = 0.0; // should not happen
			}

			FilterParams = new FilterParams(
				filterParameters: new[] { selectedField, selectedComponent, value.ToString(CultureInfo.InvariantCulture) },
				keyTimeSteps: new decimal[0], // no key time steps for now
				compressionParameters: new string[0], // no compression for now
				layerName: string.IsNullOrWhiteSpace(layerNameText) ? null : layerNameText,
				constraintFieldName: null
			);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void comboBoxField_SelectedIndexChanged(object sender, EventArgs e)
		{
			setupComponents();
			updateLayerName();
		}

		private void comboBoxComponent_SelectedIndexChanged(object sender, EventArgs e)
		{
			updateLayerName();
		}

		private void textBoxValue_TextChanged(object sender, EventArgs e)
		{
			updateLayerName();
		}
	}
}
