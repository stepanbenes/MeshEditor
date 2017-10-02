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
				setupControlPoints();
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
			setupControlPoints();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void setupControlPoints()
		{
			controlPointsPanel.Controls.Clear();
			if (ColorScale != null)
			{
				int controlTop = 2;
				foreach (var controlPoint in ColorScale.ControlPoints.Reverse())
				{
					var controlPointSetter = new ColorScaleControlPointSetter(controlPoint)
					{
						Top = controlTop
					};
					controlPointsPanel.Controls.Add(controlPointSetter);
					controlTop += controlPointSetter.Height;
				}
			}
			//buttonRemove.Enabled = ColorScale.ControlPoints.Any();
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
