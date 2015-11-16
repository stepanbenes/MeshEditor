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
		IEnumerable<Property> propertiesToShow;
        IReadOnlyDictionary<Property, Color> savedPropertyColors;

		string propertyColorsConfigFilePath;

        public ConfigurePropertyColorsForm(string propertyColorsConfigFilePath, IEnumerable<SceneFacade> scenes, IEnumerable<Property> propertiesToShow)
		{
			InitializeComponent();

			this.propertiesToShow = propertiesToShow ?? PropertyColorProvider.GetAllUsedPropertiesSorted();
			this.scenes = scenes;
			this.propertyColorsConfigFilePath = propertyColorsConfigFilePath;

            savePropertyColors();
			initPropertyPanel();
		}

		private void initPropertyPanel()
		{
			contentPanel.Controls.Clear();
			int controlTop = 2;
			foreach (Property property in propertiesToShow)
			{
				var color = PropertyColorProvider.Get(property);

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

		private void savePropertyColors()
		{
			savedPropertyColors = PropertyColorProvider.GetAllPropertyColors();
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
				PropertyColorProvider.UpdatePropertyColors(savedPropertyColors);
				updateColorBuffers();
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			PropertyColorProvider.SavePropertyColorsToFile(propertyColorsConfigFilePath);
			savePropertyColors();
			isDataDirty = false;
		}

		private void buttonReset_Click(object sender, EventArgs e)
		{
			PropertyColorProvider.ResetToDefaults();
			isDataDirty = true;
			initPropertyPanel();
			updateColorBuffers();
		}
	}
}
