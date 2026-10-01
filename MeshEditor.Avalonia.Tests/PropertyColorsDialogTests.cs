using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Controls;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Ports of ConfigurePropertyColorsForm and PropertyColorControl, and access keys of tool strip drop-downs.
	/// </summary>
	public class PropertyColorsDialogTests
	{
		private static ColorPickerDialog waitForColorDialog(Window owner)
		{
			for (int i = 0; i < 200; i++)
			{
				Dispatcher.UIThread.RunJobs();
				var dialog = owner.OwnedWindows.OfType<ColorPickerDialog>().FirstOrDefault();
				if (dialog != null)
					return dialog;
			}
			Assert.Fail("color dialog was not opened");
			return null;
		}

		[AvaloniaTest]
		public async Task Color_change_is_applied_and_cancel_restores_it()
		{
			var property = new Property(1);
			var originalColor = PropertyColorProvider.Get(property);

			var dialog = new ConfigurePropertyColorsDialog(Array.Empty<SceneFacade>(), new[] { new Property(1), new Property(2) });
			dialog.Show();
			Dispatcher.UIThread.RunJobs();

			var controls = dialog.PropertyColorControls.ToList();
			Assert.That(controls.Select(c => c.Label.Text), Is.EqualTo(new[] { "Property 1", "Property 2" }));
			Assert.That(controls[0].Color.ToArgb(), Is.EqualTo(originalColor.ToArgb()));

			// click the color -> color dialog -> choose a color
			Task click = controls[0].pictureBoxPropertyColor_Click();
			var colorDialog = waitForColorDialog(dialog);
			var newColor = originalColor.ToArgb() == System.Drawing.Color.FromArgb(255, 10, 20, 30).ToArgb() ? Colors.Orange : Color.FromRgb(10, 20, 30);
			colorDialog.Color = newColor;
			colorDialog.buttonOK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			await click;

			Assert.That(PropertyColorProvider.Get(property).ToArgb(), Is.EqualTo(ColorPickerDialog.ToDrawingColor(newColor).ToArgb()), "applied immediately");
			Assert.That(((ISolidColorBrush)controls[0].ColorBox.Background).Color, Is.EqualTo(newColor));

			dialog.buttonCancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.IsVisible, Is.False);
			Assert.That(PropertyColorProvider.Get(property).ToArgb(), Is.EqualTo(originalColor.ToArgb()), "cancel restores the saved colors");
		}

		[AvaloniaTest]
		public async Task Cancelled_color_dialog_changes_nothing()
		{
			var property = new Property(2);
			var originalColor = PropertyColorProvider.Get(property);
			var dialog = new ConfigurePropertyColorsDialog(Array.Empty<SceneFacade>(), new[] { property });
			dialog.Show();
			Dispatcher.UIThread.RunJobs();

			var control = dialog.PropertyColorControls.Single();
			Task click = control.pictureBoxPropertyColor_Click();
			var colorDialog = waitForColorDialog(dialog);
			colorDialog.Color = Colors.Blue;
			colorDialog.buttonCancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			await click;

			Assert.That(control.Color.ToArgb(), Is.EqualTo(originalColor.ToArgb()));
			Assert.That(PropertyColorProvider.Get(property).ToArgb(), Is.EqualTo(originalColor.ToArgb()));
			dialog.Close();
		}

		[TestCase("_Configure property colors", 'c')]
		[TestCase("Ed_ge property colors", 'g')]
		[TestCase("No access key", null)]
		[TestCase("Escaped __underscore and _key", 'k')]
		public void Access_key_of_header(string header, char? expected)
		{
			Assert.That(FlyoutAccessKeys.AccessKey(header), Is.EqualTo(expected));
		}
	}
}
