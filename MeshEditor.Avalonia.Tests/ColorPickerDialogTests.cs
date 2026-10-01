using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MeshEditor.AvaloniaUI.Dialogs;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	public class ColorPickerDialogTests
	{
		private static string render(ColorPickerDialog dialog, string name)
		{
			Dispatcher.UIThread.RunJobs();
			string path = Path.Combine(Path.GetTempPath(), name);
			dialog.CaptureRenderedFrame().Save(path);
			return path;
		}

		[AvaloniaTest]
		public void Text_tabs_replace_the_broken_icon_tab_strip_and_switch_pages()
		{
			var dialog = new ColorPickerDialog { Color = Avalonia.Media.Colors.Orange };
			dialog.Show();
			TestContext.Out.WriteLine(render(dialog, "colorpicker-spectrum.png"));

			var tabBackground = dialog.colorView.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "TabBackgroundBorder");
			Assert.That(tabBackground.IsVisible, Is.False, "the empty grey bar is hidden");
			Assert.That(dialog.pageTabs.ItemCount, Is.EqualTo(3));

			dialog.pageTabs.SelectedIndex = 2;
			Assert.That(dialog.colorView.SelectedIndex, Is.EqualTo(2), "Components page (RGB/HSV/hex input)");
			TestContext.Out.WriteLine(render(dialog, "colorpicker-components.png"));

			dialog.pageTabs.SelectedIndex = 1;
			Assert.That(dialog.colorView.SelectedIndex, Is.EqualTo(1));
		}
	}
}
