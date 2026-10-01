using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Ports of CheckedInputValueForm and PropertyInputForm and their use by the viewport.
	/// </summary>
	public class PropertyDialogsTests
	{
		private static string samplePath => Path.Combine(AppContext.BaseDirectory, "TestData", "sample.obj");

		/// <summary>
		/// Windows opened by the application during a test.
		/// </summary>
		private readonly List<Window> openedWindows = new List<Window>();

		[SetUp]
		public void SetUp()
		{
			openedWindows.Clear();
			Window.WindowOpenedEvent.AddClassHandler<Window>((w, e) => openedWindows.Add(w), RoutingStrategies.Direct);
		}

		private T waitForWindow<T>() where T : Window
		{
			for (int i = 0; i < 200; i++)
			{
				Dispatcher.UIThread.RunJobs();
				var window = openedWindows.OfType<T>().LastOrDefault(w => w.IsVisible);
				if (window != null)
					return window;
			}
			Assert.Fail($"{typeof(T).Name} was not opened");
			return null;
		}

		private static void enter(Window window, string text)
		{
			var textBox = window.GetLogicalDescendants().OfType<TextBox>().First();
			textBox.Text = text;
			window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
			Dispatcher.UIThread.RunJobs();
		}

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
		public void CheckedInputValueDialog_shows_check_box_between_text_and_value()
		{
			InputValueDialog.SavedValue = "11";
			var dialog = new CheckedInputValueDialog("Insert property number", "Select nodes by property:", "add to selection") { IsChecked = true };
			dialog.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.CheckBox.Content, Is.EqualTo("add to selection"));
			Assert.That(dialog.IsChecked, Is.True);
			Assert.That(dialog.textBoxValue.Text, Is.EqualTo("11"), "shares the saved value with InputValueDialog like CheckedInputValueForm");
			double checkBoxTop = dialog.CheckBox.TranslatePoint(default, dialog).Value.Y;
			Assert.That(checkBoxTop, Is.GreaterThan(dialog.labelText.TranslatePoint(default, dialog).Value.Y));
			Assert.That(checkBoxTop, Is.LessThan(dialog.textBoxValue.TranslatePoint(default, dialog).Value.Y));
		}

		[AvaloniaTest]
		public void PropertyInputDialog_remembers_its_own_value()
		{
			InputValueDialog.SavedValue = "1";
			PropertyInputDialog.SavedValue = "2";
			var dialog = new PropertyInputDialog("Insert property number", "Nothing selected", SceneFacade.GetEmptyScene());
			Task<bool> result = dialog.ShowDialog(null);
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.textBoxValue.Text, Is.EqualTo("2"));
			Assert.That(dialog.EditPropertyCommentLink.Content, Is.EqualTo("Edit property comment"));

			enter(dialog, "42");

			Assert.That(result.IsCompleted && result.Result, Is.True);
			Assert.That(PropertyInputDialog.SavedValue, Is.EqualTo("42"));
			Assert.That(InputValueDialog.SavedValue, Is.EqualTo("1"), "InputValueDialog.SavedValue is independent");
		}

		[AvaloniaTest]
		public async Task Set_property_of_selected_items_and_select_by_property()
		{
			var window = await openWindowWithSample();
			var viewport = window.ActiveViewport;
			var scene = viewport.SceneFacade;
			SceneFacade.EditorMode = EditorMode.SelectElements;
			scene.PerformAction(AvailableAction.SelectAllItems);

			// Enter / tool strip button -> PropertyInputForm
			Task setProperty = viewport.SetPropertyOfSelectedItems();
			var propertyDialog = waitForWindow<PropertyInputDialog>();
			Assert.That(propertyDialog.labelText.Text, Does.StartWith("4 elements"), "description of the selected elements (sample.obj has 4 triangles)");
			enter(propertyDialog, "7");
			await setProperty;

			var properties = ((IEnumerable<Property>)scene.GetValue(AvailableValue.AllMeshPropertiesSorted)).ToList();
			Assert.That(properties, Does.Contain(new Property(7)));
			Assert.That((bool)scene.GetValue(AvailableValue.UnsavedChangesInMesh), Is.True);

			// F6 -> CheckedInputValueForm
			scene.PerformAction(AvailableAction.UnselectAllItems);
			Assert.That((string)scene.GetValue(AvailableValue.SelectedItemsDescription), Is.Empty);
			Task select = viewport.SelectItemsWithProperty("elements");
			var selectDialog = waitForWindow<CheckedInputValueDialog>();
			Assert.That(selectDialog.labelText.Text, Is.EqualTo("Select elements by property:"));
			Assert.That(selectDialog.IsChecked, Is.False);
			enter(selectDialog, "7");
			await select;

			Assert.That((string)scene.GetValue(AvailableValue.SelectedItemsDescription), Does.StartWith("4 elements"), "elements with property 7 are selected again");
		}

		[AvaloniaTest]
		public async Task Select_by_property_rejects_negative_numbers()
		{
			var window = await openWindowWithSample();
			Task select = window.ActiveViewport.SelectItemsWithProperty("elements", addToSelection: true);
			var selectDialog = waitForWindow<CheckedInputValueDialog>();
			Assert.That(selectDialog.IsChecked, Is.True, "Shift+F6 variant");

			enter(selectDialog, "-1");

			var errorBox = waitForWindow<MessageBox>();
			Assert.That(errorBox.Title, Is.EqualTo("Inserted value is not an integer"));
			Assert.That(select.IsCompleted, Is.False);
			errorBox.Close();
			selectDialog.Close();
			Dispatcher.UIThread.RunJobs();
		}
	}
}
