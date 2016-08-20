using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class VisualizerSettingsControl : UserControl
	{
		IVisualizerSettings settings;
		bool updatingView;

		public VisualizerSettingsControl()
		{
			InitializeComponent();
			updateView();
			comboBoxNumberOfSubIntervals.Items.AddRange(Enumerable.Range(1, 5).Cast<object>().ToArray());
		}

		public IVisualizerSettings Settings
		{
			get { return settings; }
			set
			{
				if (settings != value)
				{
					settings = value;
					updateView();
				}
			}
		}

		public event EventHandler SettingsChanged;

		private void updateView()
		{
			if (settings == null)
			{
				Enabled = false;
				return;
			}
			Enabled = true;
			try
			{
				updatingView = true;
				checkBoxShowIsoAreas.Checked = settings.DrawIsoAreas;
				comboBoxNumberOfSubIntervals.SelectedItem = settings.IsoAreasSubIntervalNumber;
				
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
			Settings.DrawIsoAreas = checkBoxShowIsoAreas.Checked;
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void comboBoxNumberOfSubIntervals_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (updatingView)
				return;
			Settings.IsoAreasSubIntervalNumber = (int)comboBoxNumberOfSubIntervals.SelectedItem;
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void linkLabelEditColorScale_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			EditColorScaleForm editColorScaleForm = new EditColorScaleForm
			{
				Owner = Application.OpenForms?[0],
				ColorScale = new ColorScale(Settings.ColorScale)
			};
			if (editColorScaleForm.ShowDialog() == DialogResult.OK)
			{
				Settings.ColorScale = editColorScaleForm.ColorScale;
				SettingsChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
