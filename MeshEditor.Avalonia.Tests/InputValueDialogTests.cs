using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Dialogs;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Behaviour of the port of WinForms InputValueForm.
	/// </summary>
	public class InputValueDialogTests
	{
		private static Window showOwner()
		{
			var owner = new Window { Width = 800, Height = 600 };
			owner.Show();
			return owner;
		}

		private static InputValueDialog createIntegerDialog(string boxValue)
		{
			var dialog = new InputValueDialog("Insert element ID", "Signal element with ID:", boxValue);
			dialog.InputValueValidating += (s, e) =>
			{
				if (!int.TryParse(dialog.InputValue, out _))
					e.Reject("Inserted value is not an integer", "Please input valid integer value.");
			};
			return dialog;
		}

		private static void press(Window window, Key key, PhysicalKey physicalKey)
		{
			window.KeyPressQwerty(physicalKey, RawInputModifiers.None);
			Dispatcher.UIThread.RunJobs();
			if (window.IsVisible) // the key press may have closed the window
				window.KeyReleaseQwerty(physicalKey, RawInputModifiers.None);
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void Shows_caption_info_text_rows_and_saved_value()
		{
			InputValueDialog.SavedValue = "42";
			var dialog = new InputValueDialog("My caption", "first row | second row");
			dialog.Show(showOwner());
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.Title, Is.EqualTo("My caption"));
			Assert.That(dialog.labelText.Text, Is.EqualTo("first row" + Environment.NewLine + "second row"));
			Assert.That(dialog.textBoxValue.Text, Is.EqualTo("42"));
			Assert.That(dialog.textBoxValue.IsFocused, Is.True, "text box has focus");
			Assert.That(dialog.textBoxValue.SelectedText, Is.EqualTo("42"), "value is selected");
			Assert.That(dialog.ClientSize.Width, Is.GreaterThanOrEqualTo(360), "minimum width like the WinForms form");
		}

		[AvaloniaTest]
		public void Enter_with_valid_value_confirms_and_remembers_value()
		{
			InputValueDialog.SavedValue = "0";
			var dialog = createIntegerDialog("0");
			Task<bool> result = dialog.ShowDialog(showOwner());
			Dispatcher.UIThread.RunJobs();

			dialog.textBoxValue.Text = "17";
			press(dialog, Key.Enter, PhysicalKey.Enter);

			Assert.That(result.IsCompleted, Is.True, "dialog closed");
			Assert.That(result.Result, Is.True, "DialogResult.OK");
			Assert.That(InputValueDialog.SavedValue, Is.EqualTo("17"));
		}

		[AvaloniaTest]
		public void Ok_button_confirms()
		{
			var dialog = createIntegerDialog("5");
			Task<bool> result = dialog.ShowDialog(showOwner());
			Dispatcher.UIThread.RunJobs();

			dialog.buttonOK.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(result.IsCompleted && result.Result, Is.True);
		}

		[AvaloniaTest]
		public void Escape_cancels_and_keeps_saved_value()
		{
			InputValueDialog.SavedValue = "3";
			var dialog = createIntegerDialog("3");
			Task<bool> result = dialog.ShowDialog(showOwner());
			Dispatcher.UIThread.RunJobs();

			dialog.textBoxValue.Text = "99";
			press(dialog, Key.Escape, PhysicalKey.Escape);

			Assert.That(result.IsCompleted, Is.True, "dialog closed");
			Assert.That(result.Result, Is.False, "DialogResult.Cancel");
			Assert.That(InputValueDialog.SavedValue, Is.EqualTo("3"));
		}

		[AvaloniaTest]
		public void Invalid_value_shows_error_and_keeps_dialog_open()
		{
			var dialog = createIntegerDialog("0");
			Task<bool> result = dialog.ShowDialog(showOwner());
			Dispatcher.UIThread.RunJobs();

			dialog.textBoxValue.Text = "abc";
			press(dialog, Key.Enter, PhysicalKey.Enter);

			var errorBox = dialog.OwnedWindows.OfType<MessageBox>().SingleOrDefault();
			Assert.That(errorBox, Is.Not.Null, "error message box is shown");
			Assert.That(errorBox.Title, Is.EqualTo("Inserted value is not an integer"));
			Assert.That(errorBox.GetLogicalDescendants().OfType<SelectableTextBlock>().Single().Text, Is.EqualTo("Please input valid integer value."));
			Assert.That(result.IsCompleted, Is.False, "input dialog stays open");

			// confirm the error, the value gets selected again for correction
			press(errorBox, Key.Enter, PhysicalKey.Enter);
			Assert.That(errorBox.IsVisible, Is.False);
			Assert.That(result.IsCompleted, Is.False);
			Assert.That(dialog.textBoxValue.SelectedText, Is.EqualTo("abc"));

			dialog.textBoxValue.Text = "8";
			press(dialog, Key.Enter, PhysicalKey.Enter);
			Assert.That(result.IsCompleted && result.Result, Is.True);
		}
	}
}
