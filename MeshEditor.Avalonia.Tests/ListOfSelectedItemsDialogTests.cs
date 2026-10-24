using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.CoreInterface;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Port of ListOfSelectedItemsForm.
	/// </summary>
	public class ListOfSelectedItemsDialogTests
	{
		private static string samplePath => Path.Combine(AppContext.BaseDirectory, "TestData", "sample.obj");

		private static async Task<MainWindow> openWindowWithSample()
		{
			var window = new MainWindow(new[] { samplePath }) { WindowState = WindowState.Normal, Width = 800, Height = 600 };
			window.Show();
			var timeout = DateTime.UtcNow.AddSeconds(30);
			while (!window.ActiveViewport.SceneFacade.ContainsMesh || window.ActiveViewport.IOActionInProgress)
			{
				if (DateTime.UtcNow > timeout)
					Assert.Fail("mesh was not loaded in time");
				Dispatcher.UIThread.RunJobs();
				await Task.Delay(20);
			}
			return window;
		}

		[AvaloniaTest]
		public async Task Lists_selected_elements_and_switches_entity_type()
		{
			var window = await openWindowWithSample();
			var scene = window.ActiveViewport.SceneFacade;
			SceneFacade.EditorMode = EditorMode.SelectElements;
			scene.PerformAction(AvailableAction.SelectAllItems);

			var dialog = new ListOfSelectedItemsDialog(scene);
			dialog.Show(window);
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.comboBoxEntityType.SelectedIndex, Is.EqualTo(1), "elements are selected, so the list starts with elements");
			Assert.That(dialog.labelItems.Text, Is.EqualTo("(4 elements)"));
			Assert.That(dialog.richTextBox.Text, Is.Not.Empty);
			Assert.That(dialog.linkLabelAddProperty.IsVisible, Is.False, "property links are only for nodes");

			string complete = dialog.richTextBox.Text;
			dialog.checkBoxShowCompleteInfo.IsChecked = false;
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.richTextBox.Text.Length, Is.LessThan(complete.Length), "short info without complete details");

			dialog.comboBoxEntityType.SelectedIndex = 0;
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.linkLabelAddProperty.IsVisible && dialog.linkLabelRemoveProperty.IsVisible, Is.True);
		}

		[AvaloniaTest]
		public async Task Add_property_to_selected_nodes()
		{
			var window = await openWindowWithSample();
			var scene = window.ActiveViewport.SceneFacade;
			SceneFacade.EditorMode = EditorMode.SelectNodes;
			scene.PerformAction(AvailableAction.SelectAllItems);

			var dialog = new ListOfSelectedItemsDialog(scene);
			dialog.Show(window);
			Dispatcher.UIThread.RunJobs();
			Assert.That(dialog.comboBoxEntityType.SelectedIndex, Is.EqualTo(0));
			Assert.That(dialog.richTextBox.Text, Does.Not.Contain("33"));

			InputValueDialog input = null;
			Window.WindowOpenedEvent.AddClassHandler<InputValueDialog>((w, e) => input ??= w);
			dialog.linkLabelAddProperty.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(input, Is.Not.Null, "property input dialog is shown");
			Assert.That(input.labelText.Text, Is.EqualTo("Specify property to be added"));

			input.textBoxValue.Text = "33";
			input.buttonOK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.richTextBox.Text, Does.Contain("33"), "list is refreshed with the added property");
		}
	}
}
