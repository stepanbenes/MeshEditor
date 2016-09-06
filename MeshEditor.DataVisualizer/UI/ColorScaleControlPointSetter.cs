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
	public partial class ColorScaleControlPointSetter : UserControl
	{
		ColorScale.ControlPoint controlPoint;

		public ColorScaleControlPointSetter(ColorScale.ControlPoint controlPoint)
		{
			InitializeComponent();

			this.controlPoint = controlPoint;

			checkBoxIsFixed.Checked = controlPoint.IsFixed;
			textBoxValue.Text = controlPoint.Value.ToString();
			textBoxValue.Enabled = controlPoint.IsFixed;
			pictureBoxColor.BackColor = Utilities.Functions.ColorFromRgba32(controlPoint.Color);
		}

		private void pictureBoxPropertyColor_Click(object sender, EventArgs e)
		{
			var pictureBox = sender as PictureBox;
			if (pictureBox != null)
			{
				var colorPicker = new ColorDialog();
				colorPicker.Color = pictureBox.BackColor;
				colorPicker.FullOpen = true;
				if (colorPicker.ShowDialog() == DialogResult.OK)
				{
					pictureBox.BackColor = colorPicker.Color;
					controlPoint.Color = Utilities.Functions.ColorToRgba32(colorPicker.Color);
				}
			}
		}

		private void textBoxValue_TextChanged(object sender, EventArgs e)
		{
			double value;
			if (double.TryParse(textBoxValue.Text, out value))
			{
				controlPoint.Value = value;
			}
		}

		private void checkBoxIsFixed_CheckedChanged(object sender, EventArgs e)
		{
			controlPoint.IsFixed = checkBoxIsFixed.Checked;
			textBoxValue.Enabled = controlPoint.IsFixed;
		}
	}
}
