using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MeshEditor.AvaloniaUI.Controls;
using MeshEditor.CoreInterface;
using MeshEditor.Data;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Colors of the properties used for the property color modes (port of MeshEditor.WinUI.ConfigurePropertyColorsForm).
	/// </summary>
	public partial class ConfigurePropertyColorsDialog : Window
	{
		private readonly IEnumerable<SceneFacade> scenes;
		private bool isDataDirty = false;
		private readonly IEnumerable<Property> propertiesToShow;
		private IReadOnlyDictionary<Property, System.Drawing.Color> savedPropertyColors;
		private bool closedByButton;

		public ConfigurePropertyColorsDialog()
			: this(Array.Empty<SceneFacade>(), Array.Empty<Property>())
		{ }

		public ConfigurePropertyColorsDialog(IEnumerable<SceneFacade> scenes, IEnumerable<Property> propertiesToShow)
		{
			InitializeComponent();
			this.propertiesToShow = propertiesToShow ?? PropertyColorProvider.GetAllUsedPropertiesSorted();
			this.scenes = scenes;
			savePropertyColors();
			initPropertyPanel();
		}

		internal IEnumerable<PropertyColorControl> PropertyColorControls => contentPanel.Children.OfType<PropertyColorControl>();

		public new async Task ShowDialog(Window owner)
		{
			owner ??= DialogOwner.Current;
			if (owner != null)
				await base.ShowDialog(owner);
			else
			{
				var closed = new TaskCompletionSource();
				Closed += (s, e) => closed.TrySetResult();
				WindowStartupLocation = WindowStartupLocation.CenterScreen;
				Show();
				await closed.Task;
			}
		}

		private void initPropertyPanel()
		{
			contentPanel.Children.Clear();
			foreach (Property property in propertiesToShow)
			{
				var color = PropertyColorProvider.Get(property);
				var propertyColorControl = new PropertyColorControl(property, color);
				propertyColorControl.ColorChanged += (sender, args) =>
				{
					if (sender is PropertyColorControl control)
					{
						PropertyColorProvider.Set(control.Property, control.Color);
						updateColorBuffers();
						isDataDirty = true;
					}
				};
				contentPanel.Children.Add(propertyColorControl);
			}
		}

		private void savePropertyColors()
		{
			savedPropertyColors = PropertyColorProvider.GetAllPropertyColors();
		}

		private void updateColorBuffers()
		{
			Cursor = new Cursor(StandardCursorType.Wait);
			foreach (SceneFacade scene in scenes)
			{
				scene.PerformAction(AvailableAction.UpdateColorBuffers);
			}
			Cursor = Cursor.Default;
		}

		private void cancel()
		{
			if (isDataDirty)
			{
				PropertyColorProvider.UpdatePropertyColors(savedPropertyColors);
				updateColorBuffers();
				isDataDirty = false;
			}
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			cancel();
			closedByButton = true;
			Close();
		}

		private void buttonOK_Click(object sender, RoutedEventArgs e)
		{
			PropertyColorProvider.SavePropertyColors();
			savePropertyColors();
			isDataDirty = false;
			closedByButton = true;
			Close();
		}

		private void buttonReset_Click(object sender, RoutedEventArgs e)
		{
			PropertyColorProvider.ResetToDefaults();
			isDataDirty = true;
			initPropertyPanel();
			updateColorBuffers();
		}

		protected override void OnClosed(EventArgs e)
		{
			// closing by the title bar button is Cancel (DialogResult.Cancel) like in the WinForms form
			if (!closedByButton)
				cancel();
			base.OnClosed(e);
		}
	}
}
