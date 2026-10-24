using System.IO;
using MeshEditor.AvaloniaUI.Rendering;
using NUnit.Framework;
using SkiaSharp;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Screenshots are saved in the format given by the file extension (formerly by ImageSharp).
	/// </summary>
	public class ScreenshotFileTests
	{
		private const int width = 33, height = 24; // odd width: BMP rows need padding

		/// <summary>
		/// Top-down BGRA pixels: blue top left quarter, red bottom right quarter, gray elsewhere
		/// (blocks, not single pixels: JPEG subsamples the colors).
		/// </summary>
		private static byte[] createPixels()
		{
			byte[] bgra = new byte[width * height * 4];
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					int i = (y * width + x) * 4;
					bool top = y < height / 2, left = x < width / 2;
					(byte b, byte g, byte r) = top && left ? ((byte)255, (byte)0, (byte)0) : !top && !left ? ((byte)0, (byte)0, (byte)255) : ((byte)128, (byte)128, (byte)128);
					bgra[i] = b; bgra[i + 1] = g; bgra[i + 2] = r; bgra[i + 3] = 255;
				}
			return bgra;
		}

		[TestCase(".png", 0)]
		[TestCase(".bmp", 0)]
		[TestCase(".jpg", 24)] // lossy
		[TestCase(".jpeg", 24)]
		public void Saved_image_has_the_pixels_in_the_right_orientation(string extension, int tolerance)
		{
			string path = Path.Combine(Path.GetTempPath(), "mesheditor-screenshot-test" + extension);
			try
			{
				ScreenshotFile.Save(path, createPixels(), width, height);

				using var bitmap = SKBitmap.Decode(path);
				Assert.That(bitmap, Is.Not.Null, "file can be decoded");
				Assert.That((bitmap.Width, bitmap.Height), Is.EqualTo((width, height)));
				assertColor(bitmap.GetPixel(width / 4, height / 4), 0, 0, 255, tolerance, "top left is blue");
				assertColor(bitmap.GetPixel(width * 3 / 4, height * 3 / 4), 255, 0, 0, tolerance, "bottom right is red");
				assertColor(bitmap.GetPixel(width * 3 / 4, height / 4), 128, 128, 128, tolerance, "top right is gray");
				assertColor(bitmap.GetPixel(width - 1, 0), 128, 128, 128, tolerance, "last column of the first row is gray");
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void Supported_extensions_match_the_screenshot_dialog_filter()
		{
			foreach (var extension in new[] { ".png", ".PNG", ".jpg", ".jpeg", ".bmp" })
				Assert.That(ScreenshotFile.IsSupportedExtension(extension), Is.True, extension);
			foreach (var extension in new[] { ".gif", ".tif", "" })
				Assert.That(ScreenshotFile.IsSupportedExtension(extension), Is.False, extension);
		}

		private static void assertColor(SKColor color, int r, int g, int b, int tolerance, string message)
		{
			Assert.That(color.Red, Is.InRange(r - tolerance, r + tolerance), message + " (red)");
			Assert.That(color.Green, Is.InRange(g - tolerance, g + tolerance), message + " (green)");
			Assert.That(color.Blue, Is.InRange(b - tolerance, b + tolerance), message + " (blue)");
		}
	}
}
