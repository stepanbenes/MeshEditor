using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MeshEditor.AvaloniaUI.Dialogs;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	public class ColorPickerDialogTests
	{
		private static Bitmap render(ColorPickerDialog dialog, string name)
		{
			Dispatcher.UIThread.RunJobs();
			var frame = dialog.CaptureRenderedFrame();
			string path = Path.Combine(Path.GetTempPath(), name);
			frame.Save(path, PngBitmapEncoderOptions.Default);
			TestContext.Out.WriteLine(path);
			return frame;
		}

		/// <summary>
		/// Number of dark pixels (icon strokes) of the frame in the area of the control.
		/// </summary>
		private static int countDarkPixels(Bitmap frame, Visual root, Control control)
		{
			int width = frame.PixelSize.Width, height = frame.PixelSize.Height;
			byte[] bgra = new byte[width * height * 4];
			var handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
			try
			{
				frame.CopyPixels(new PixelRect(0, 0, width, height), handle.AddrOfPinnedObject(), bgra.Length, width * 4);
			}
			finally
			{
				handle.Free();
			}
			double scale = width / root.Bounds.Width;
			var topLeft = control.TranslatePoint(new Point(0, 0), root) ?? throw new InvalidOperationException("control is not in the dialog");
			int x0 = (int)(topLeft.X * scale), y0 = (int)(topLeft.Y * scale);
			int x1 = Math.Min(width, (int)((topLeft.X + control.Bounds.Width) * scale)), y1 = Math.Min(height, (int)((topLeft.Y + control.Bounds.Height) * scale));
			int count = 0;
			for (int y = y0; y < y1; y++)
				for (int x = x0; x < x1; x++)
				{
					int i = (y * width + x) * 4;
					if (bgra[i] < 100 && bgra[i + 1] < 100 && bgra[i + 2] < 100)
						count++;
				}
			return count;
		}

		[AvaloniaTest]
		public void Built_in_tab_strip_shows_icons_and_switches_pages()
		{
			var dialog = new ColorPickerDialog { Color = Avalonia.Media.Colors.Orange };
			dialog.Show();
			var frame = render(dialog, "colorpicker-spectrum.png");

			// the icons of the tab strip were not rendered with Avalonia 12.1.3 in the app (an empty grey bar)
			var tabControl = dialog.colorView.GetVisualDescendants().OfType<TabControl>().Single(t => t.Name == "PART_TabControl");
			var tabs = tabControl.GetVisualDescendants().OfType<TabItem>().Where(t => t.IsVisible).ToList();
			Assert.That(tabs, Has.Count.EqualTo(3), "Spectrum, Palette and Components");
			foreach (var tab in tabs)
				Assert.That(countDarkPixels(frame, dialog, tab), Is.GreaterThan(20), $"icon of tab {tabs.IndexOf(tab)} is drawn");

			dialog.colorView.SelectedIndex = 2;
			Assert.That(tabControl.SelectedIndex, Is.EqualTo(2), "Components page (RGB/HSV/hex input)");
			render(dialog, "colorpicker-components.png");
		}
	}
}
