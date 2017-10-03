using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class ColorScaleSettingsForm : Form
	{
		IVisualizerSettings originalSettings, currentSettings;
		bool updatingView;

		public ColorScaleSettingsForm(IVisualizerSettings settings)
		{
			InitializeComponent();
			comboBoxColorScaleType.Items.AddRange(Enum.GetValues(typeof(ColorScale.Types)).Cast<object>().ToArray());
			comboBoxNumberOfSubIntervals.Items.AddRange(Enumerable.Range(1, 5).Cast<object>().ToArray());
			originalSettings = settings;
			currentSettings = new VisualizerSettings();
			copyProperties(originalSettings, currentSettings);
			updateView();
		}

		public event EventHandler SettingsChanged;

		private void updateView()
		{
			try
			{
				updatingView = true;
				checkBoxShowIsoAreas.Checked = originalSettings.DrawIsoAreas;
				comboBoxNumberOfSubIntervals.SelectedItem = originalSettings.IsoAreasSubIntervalNumber;
				comboBoxColorScaleType.SelectedItem = originalSettings.ColorScale?.Type;
				setupControlPoints();
			}
			finally
			{
				updatingView = false;
			}
		}

		private void checkBoxShowIsoAreas_CheckedChanged(object sender, EventArgs e)
		{
			if (updatingView)
				return;
			currentSettings.DrawIsoAreas = checkBoxShowIsoAreas.Checked;
		}

		private void comboBoxNumberOfSubIntervals_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (updatingView)
				return;
			currentSettings.IsoAreasSubIntervalNumber = (int)comboBoxNumberOfSubIntervals.SelectedItem;
		}

		private void comboBoxColorScaleType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (updatingView)
				return;
			currentSettings.ColorScale.Type = (ColorScale.Types)comboBoxColorScaleType.SelectedItem;
			setupControlPoints();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			copyProperties(currentSettings, originalSettings);
			SettingsChanged?.Invoke(this, EventArgs.Empty);

			DialogResult = DialogResult.OK; // closes dialog
		}

		private void buttonApply_Click(object sender, EventArgs e)
		{
			copyProperties(currentSettings, originalSettings);
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void setupControlPoints()
		{
			controlPointsPanel.Controls.Clear();
			int controlTop = 2;
			foreach (var controlPoint in currentSettings.ColorScale.ControlPoints.Reverse())
			{
				var controlPointSetter = new ColorScaleControlPointSetter(controlPoint)
				{
					Top = controlTop
				};
				controlPointsPanel.Controls.Add(controlPointSetter);
				controlTop += controlPointSetter.Height;
			}
			//buttonRemove.Enabled = ColorScale.ControlPoints.Any();
		}

		private static void copyProperties(IVisualizerSettings source, IVisualizerSettings destination)
		{
			destination.ColorScale = source.ColorScale;
			destination.DrawIsoAreas = source.DrawIsoAreas;
			destination.IsoAreasSubIntervalNumber = source.IsoAreasSubIntervalNumber;
		}

		//private void buttonAdd_Click(object sender, EventArgs e)
		//{
		//	var newControlPoint = new ColorScale.ControlPoint(ColorScale.UndefinedValueColor);
		//	ColorScale.AddNewControlPoint(newControlPoint);
		//	setupControlPoints();
		//}

		//private void buttonRemove_Click(object sender, EventArgs e)
		//{
		//	var lastControlPoint = ColorScale.ControlPoints.LastOrDefault();
		//	if (lastControlPoint != null)
		//	{
		//		ColorScale.RemoveControlPoint(lastControlPoint);
		//		setupControlPoints();
		//	}
		//}
	}
}
