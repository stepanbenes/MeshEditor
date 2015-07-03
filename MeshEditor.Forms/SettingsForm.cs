using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using MeshEditor.Graphics;
using MeshEditor.Data;
using MeshEditor.CoreInterface;

using System.Threading;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// Tento dialog zpristupni pokrocile volby programu. 
	/// Lze tu nastavit barvu pozadi okna, zpusob vykresleni oznacenych 
	/// ci neoznacenych entit a dalsi drobna nastaveni, 
	/// ktera umozni prizpusobit program pozadavkum uzivatele.
	/// </summary>
	public partial class SettingsForm : Form
	{
		private List<SceneFacade> scenes;

		public SettingsForm(List<SceneFacade> scenes)
		{
			InitializeComponent();

			this.scenes = scenes;
			init();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			this.Width += 10;
		}

		private void init()
		{
			AppSettings.SaveState();

			propertyGrid.PropertySort = PropertySort.Categorized;
						
			propertyGrid.SelectedObject = AppSettings.Instance;
			propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(propertyGrid_PropertyValueChanged);
		}

		void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			GridItem item = e.ChangedItem;

			if (item.PropertyDescriptor.Attributes.Contains(new UpdateColorBuffersAttribute()))
			{
				UpdateColorBuffers();
			}

			if (item.PropertyDescriptor.Attributes.Contains(new RecreateBuffersAttribute()))
			{
				RecreateBuffers();
			}

			// pokud vlastnost neobsahuje atribut DontRefresh, tak prekreslit
			if (!item.PropertyDescriptor.Attributes.Contains(new DontRefreshAttribute()))
			{
				RefreshAll();
			}
		}

		private void RefreshAll()
		{
			foreach (SceneFacade scene in scenes)
				scene.PerformAction(AvailableAction.Refresh);
		}

		private void UpdateColorBuffers()
		{
			this.Cursor = Cursors.WaitCursor;
			foreach (SceneFacade scene in scenes)
				scene.PerformAction(AvailableAction.UpdateColorBuffers);
			this.Cursor = Cursors.Default;
		}

		private void RecreateBuffers()
		{
			this.Cursor = Cursors.WaitCursor;
			foreach (SceneFacade scene in scenes)
				scene.PerformAction(AvailableAction.RecreateBuffers);
			this.Cursor = Cursors.Default;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			AppSettings.RestoreState();
			propertyGrid.Refresh();
			updateAll();
			this.Close();
		}
		
		private void buttonReset_Click(object sender, EventArgs e)
		{
			AppSettings.Reset();
			propertyGrid.Refresh();
			updateAll();
		}

		private void updateAll()
		{
			UpdateColorBuffers();
			RecreateBuffers();
			RefreshAll();
		}
	}
}
