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
		bool isDataDirty = false;
		Dictionary<Property, Color> previousPropertyColors = new Dictionary<Property, Color>();

		public ConfigurePropertyColorsForm(IEnumerable<SceneFacade> scenes)
		{
			InitializeComponent();

			this.scenes = scenes;
			initPropertyPanel();
		}

		private void initPropertyPanel()
		{
			int controlTop = 2;
			foreach (Property property in PropertyColorProvider.GetAllUsedPropertiesSorted())
			{
				var color = PropertyColorProvider.Get(property);

				previousPropertyColors[property] = color;

                var propertyColorControl = new PropertyColorControl(property, color);
				propertyColorControl.Top = controlTop;
				propertyColorControl.ColorChanged += (sender, args) =>
				{
					var control = sender as PropertyColorControl;
					if (control != null)
					{
						PropertyColorProvider.Set(control.Property, control.Color);
						updateColorBuffers();
						isDataDirty = true;
					}
				};
				contentPanel.Controls.Add(propertyColorControl);
				controlTop += propertyColorControl.Height;
			}
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
				PropertyColorProvider.LoadPropertyColors(previousPropertyColors);
				updateColorBuffers();
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			PropertyColorProvider.SavePropertyColorsToFile(SceneFacade.PropertyColorsConfigFileName);
			isDataDirty = false;
		}
	}
}
