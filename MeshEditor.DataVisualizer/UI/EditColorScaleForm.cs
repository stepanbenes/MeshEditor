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
	public partial class EditColorScaleForm : Form
	{
		ColorScale colorScale;
		bool updatingView;

		public EditColorScaleForm()
		{
			InitializeComponent();
			comboBoxColorScaleType.Items.AddRange(Enum.GetValues(typeof(ColorScale.Types)).Cast<object>().ToArray());
		}

		public ColorScale ColorScale
		{
			get { return colorScale; }
			set
			{
				if (colorScale != value)
				{
					colorScale = value;
					updateView();
				}
			}
		}

		private void updateView()
		{
			try
			{
				updatingView = true;
				comboBoxColorScaleType.SelectedItem = ColorScale?.Type;
				colorScaleSetter.ColorScale = ColorScale;
			}
			finally
			{
				updatingView = false;
			}
		}

		private void comboBoxColorScaleType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (updatingView)
				return;
			ColorScale.Type = (ColorScale.Types)comboBoxColorScaleType.SelectedItem;
			colorScaleSetter.SetupControlPoints();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}
	}
}
