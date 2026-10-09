using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MeshEditor.AvaloniaUI.Controls;
using MeshEditor.CoreInterface;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Advanced options of the program (colors, entity appearance, ...) edited in a property grid
	/// (port of MeshEditor.WinUI.SettingsForm).
	/// </summary>
	public partial class SettingsDialog : Window
	{
		private readonly List<SceneFacade> scenes;
		private bool closedByButton;

		public SettingsDialog()
			: this(new List<SceneFacade>())
		{ }

		public SettingsDialog(List<SceneFacade> scenes)
		{
			InitializeComponent();
			this.scenes = scenes;
			init();
		}

		public new async Task ShowDialog(Window owner)
		{
			await this.ShowModalAsync(owner);
		}

		private void init()
		{
			SceneSettings.SaveState();
			propertyGrid.SelectedObject = SceneSettings.Instance;
			propertyGrid.PropertyValueChanged += propertyGrid_PropertyValueChanged;
		}

		private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			var attributes = e.Property.Attributes;
			if (attributes.Contains(new UpdateColorBuffersAttribute()))
			{
				UpdateColorBuffers();
			}
			if (attributes.Contains(new RecreateBuffersAttribute()))
			{
				RecreateBuffers();
			}
			// redraw unless the property has the DontRefresh attribute
			if (!attributes.Contains(new DontRefreshAttribute()))
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
			Cursor = new Cursor(StandardCursorType.Wait);
			foreach (SceneFacade scene in scenes)
				scene.PerformAction(AvailableAction.UpdateColorBuffers);
			Cursor = Cursor.Default;
		}

		private void RecreateBuffers()
		{
			Cursor = new Cursor(StandardCursorType.Wait);
			foreach (SceneFacade scene in scenes)
				scene.PerformAction(AvailableAction.RecreateBuffers);
			Cursor = Cursor.Default;
		}

		private void buttonOK_Click(object sender, RoutedEventArgs e)
		{
			closedByButton = true;
			Close();
		}

		private void cancel()
		{
			SceneSettings.RestoreState();
			propertyGrid.SelectedObject = SceneSettings.Instance; // RestoreState replaces the instance
			updateAll();
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			cancel();
			closedByButton = true;
			Close();
		}

		private void buttonReset_Click(object sender, RoutedEventArgs e)
		{
			SceneSettings.Reset();
			propertyGrid.SelectedObject = SceneSettings.Instance;
			updateAll();
		}

		private void updateAll()
		{
			UpdateColorBuffers();
			RecreateBuffers();
			RefreshAll();
		}

		protected override void OnClosed(EventArgs e)
		{
			// the title bar close button is Cancel (CancelButton of the WinForms form)
			if (!closedByButton)
				cancel();
			base.OnClosed(e);
		}
	}
}
