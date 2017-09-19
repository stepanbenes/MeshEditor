using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class DeformationFilterParamsForm : FilterParamsForm
	{
		public DeformationFilterParamsForm(IEnumerable<string> availableVectorFields)
		{
			InitializeComponent();
			setupDeformationField(availableVectorFields);
			trackBarScale_ValueChanged(null, null);
		}

		private void setupDeformationField(IEnumerable<string> availableVectorFields)
		{
			var availableVectorFieldsArray = availableVectorFields.ToArray();
			comboBoxDeformationField.Items.Clear();
			comboBoxDeformationField.Items.AddRange(availableVectorFieldsArray);
			// try select correct one
			comboBoxDeformationField.SelectedItem = availableVectorFieldsArray.FirstOrDefault(field => field.StartsWith("displacement", StringComparison.InvariantCultureIgnoreCase));
		}

		private double getScaleValue() => trackBarScale.Value * 0.01;

		#region Event handlers

		private void trackBarScale_ValueChanged(object sender, EventArgs e)
		{
			double scale = getScaleValue();
			this.labelScale.Text = $"Scale: {scale:P0}";
			this.textBoxLayerName.Text = $"deformation (scale: {scale.ToString(CultureInfo.InvariantCulture)})";
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			string selectedDeformationField = comboBoxDeformationField.SelectedItem as string;
			string layerNameText = textBoxLayerName.Text;

			// build filter params
			FilterParams = new FilterParams(
				filterParameters: new[] { selectedDeformationField, getScaleValue().ToString() },
				keyTimeSteps: new decimal[0], // ignored for deformation filter
				compressionParameters: new string[0], // ignored for deformation filter
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

		#endregion

	}
}
