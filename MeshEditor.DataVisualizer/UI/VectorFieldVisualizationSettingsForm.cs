using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class VectorFieldVisualizationSettingsForm : Form
	{
		private readonly IVisualizerSettings originalSettings, appliedSettings, currentSettings;
		private bool isUpdatingArrowLengthView;

		public event EventHandler SettingsChanged;

		public VectorFieldVisualizationSettingsForm(IVisualizerSettings settings)
		{
			Debug.Assert(settings != null);
			InitializeComponent();
			appliedSettings = settings;
			originalSettings = new VisualizerSettings();
			currentSettings = new VisualizerSettings();
			copyProperties(appliedSettings, originalSettings);
			copyProperties(appliedSettings, currentSettings);
			updateView();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			copyProperties(currentSettings, appliedSettings);
			SettingsChanged?.Invoke(this, EventArgs.Empty);

			DialogResult = DialogResult.OK; // closes dialog
		}

		private void buttonApply_Click(object sender, EventArgs e)
		{
			copyProperties(currentSettings, appliedSettings);
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			copyProperties(originalSettings, appliedSettings);
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void updateView()
		{
			checkBoxInvertVectorArrows.Checked = currentSettings.InvertVectorArrows;
			updateArrowLengthFactorTextbox();
			updateTrackbarVectorLengthFactorValue();
		}

		void updateTrackbarVectorLengthFactorValue()
		{
			try
			{
				isUpdatingArrowLengthView = true;
				int valueForTrackbar = (int)(currentSettings.ArrowLengthFactor * 100m);
				valueForTrackbar = Math.Max(valueForTrackbar, trackBarVectorLengthFactor.Minimum);
				valueForTrackbar = Math.Min(valueForTrackbar, trackBarVectorLengthFactor.Maximum);
				trackBarVectorLengthFactor.Value = valueForTrackbar;
			}
			finally
			{
				isUpdatingArrowLengthView = false;
			}
		}

		private static void copyProperties(IVisualizerSettings source, IVisualizerSettings destination)
		{
			destination.ArrowLengthFactor = source.ArrowLengthFactor;
			destination.InvertVectorArrows = source.InvertVectorArrows;
			destination.IsArrowLengthFixed = source.IsArrowLengthFixed;
		}

		private void checkBoxInvertVectorArrows_CheckedChanged(object sender, EventArgs e)
		{
			currentSettings.InvertVectorArrows = checkBoxInvertVectorArrows.Checked;
		}

		private void trackBarVectorLengthFactor_ValueChanged(object sender, EventArgs e)
		{
			if (isUpdatingArrowLengthView)
				return;

			currentSettings.ArrowLengthFactor = trackBarVectorLengthFactor.Value * 0.01m;
			updateArrowLengthFactorTextbox();
		}

		private void checkBoxIsArrowLengthFixed_CheckedChanged(object sender, EventArgs e)
		{
			currentSettings.IsArrowLengthFixed = checkBoxIsArrowLengthFixed.Checked;
		}

		private void textBoxVectorLengthFactor_TextChanged(object sender, EventArgs e)
		{
			if (isUpdatingArrowLengthView)
				return;

			if (decimal.TryParse(textBoxVectorLengthFactor.Text, out decimal value))
			{
				currentSettings.ArrowLengthFactor = value;
				updateTrackbarVectorLengthFactorValue();
			}
		}

		private void updateArrowLengthFactorTextbox()
		{
			try
			{
				isUpdatingArrowLengthView = true;
				textBoxVectorLengthFactor.Text = currentSettings.ArrowLengthFactor.ToString();
			}
			finally
			{
				isUpdatingArrowLengthView = false;
			}
		}
	}
}
