using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MeshEditor.CoreInterface;
using MeshEditor.Data;

namespace MeshEditor.WinUI
{
	public partial class ConfigurePropertyColorsForm : Form
	{
		IEnumerable<SceneFacade> scenes;
		bool isDataDirty;

		public ConfigurePropertyColorsForm(IEnumerable<SceneFacade> scenes)
		{
			InitializeComponent();
			this.scenes = scenes;
		}

		private void updateColorBuffers()
		{
			Cursor = Cursors.WaitCursor;
			foreach (SceneFacade scene in scenes)
			{
				scene.PerformAction(AvailableAction.UpdateColorBuffers);
			}
			Cursor = Cursors.Default;
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			if (isDataDirty)
			{
				PropertyColorProvider.LoadPropertyColorsFromConfigFile();
				updateColorBuffers();
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			PropertyColorProvider.SavePropertyColorsToConfigFile();
			isDataDirty = false;
		}
	}
}
