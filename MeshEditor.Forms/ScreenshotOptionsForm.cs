using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MeshEditor.WinUI
{
	public partial class ScreenshotOptionsForm : Form
	{
		private static bool savedUseSelectionAreaState;

		public ScreenshotOptionsForm()
		{
			InitializeComponent();
			UseSelectionArea = savedUseSelectionAreaState;
			updateOkButtonText();
		}

		public bool UseSelectionArea
		{
			get { return radioButtonSelectionArea.Checked; }
			set
			{
				if (value)
				{
					radioButtonSelectionArea.Checked = true;
				}
				else
				{
					radioButtonWholeScene.Checked = true;
				}
			}
		}

		private void radioButtonWholeScene_CheckedChanged(object sender, EventArgs e)
		{
			updateOkButtonText();
		}

		private void updateOkButtonText()
		{
			buttonOK.Text = UseSelectionArea ? "Select" : "Save";
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			savedUseSelectionAreaState = UseSelectionArea;
		}
	}
}
