using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
		readonly IVisualizerSettings originalSettings, appliedSettings, currentSettings;
		bool updatingView;

		public ColorScaleSettingsForm(IVisualizerSettings settings)
		{
			Debug.Assert(settings != null);
			InitializeComponent();
			comboBoxColorScaleType.Items.AddRange(Enum.GetValues(typeof(ColorScale.Types)).Cast<object>().ToArray());
			comboBoxNumberOfSubIntervals.Items.AddRange(Enumerable.Range(1, 5).Cast<object>().ToArray());
			appliedSettings = settings;
			originalSettings = new VisualizerSettings();
			currentSettings = new VisualizerSettings();
			copyProperties(appliedSettings, originalSettings);
			copyProperties(appliedSettings, currentSettings);
			updateView();
		}

		public event EventHandler SettingsChanged;

		protected override void OnClosed(EventArgs e)
		{
			removeAllControlPointSetters();
		}

		private void updateView()
		{
			try
			{
				updatingView = true;
				checkBoxShowIsoAreas.Checked = currentSettings.DrawIsoAreas;
				comboBoxNumberOfSubIntervals.SelectedItem = currentSettings.IsoAreasSubIntervalNumber;
				comboBoxColorScaleType.SelectedItem = currentSettings.ColorScale?.Type;
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

		private void setupControlPoints()
		{
			removeAllControlPointSetters();
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

		private void removeAllControlPointSetters()
		{
			foreach(ColorScaleControlPointSetter control in controlPointsPanel.Controls)
			{
				control.Detach();
			}
			controlPointsPanel.Controls.Clear();
		}

		private static void copyProperties(IVisualizerSettings source, IVisualizerSettings destination)
		{
			destination.ColorScale = new ColorScale(source.ColorScale); // clone color scale object
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
