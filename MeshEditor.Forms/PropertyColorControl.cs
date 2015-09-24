using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MeshEditor.Data;

namespace MeshEditor.WinUI
{
	public partial class PropertyColorControl : UserControl
	{
		Property property;

		public PropertyColorControl(Property property, Color color)
		{
			InitializeComponent();

			this.property = property;
			labelPropertyNumber.Text = string.Format("Property {0}", property.Value);
			pictureBoxPropertyColor.BackColor = color;
		}

		public event EventHandler ColorChanged;

		public Property Property
		{
			get	{ return property; }
		}

		public Color Color
		{
			get { return pictureBoxPropertyColor.BackColor; }
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

					var handler = ColorChanged;
					if (handler != null)
						handler(this, EventArgs.Empty);
				}
			}
		}
	}
}
