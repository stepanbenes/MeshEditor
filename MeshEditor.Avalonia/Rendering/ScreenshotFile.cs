using System;
using System.IO;
using SkiaSharp;

namespace MeshEditor.AvaloniaUI.Rendering
{
	/// <summary>
	/// Saves screenshots (top-down BGRA pixels) as PNG, JPEG or BMP; the format is chosen by the file extension.
	/// </summary>
	internal static class ScreenshotFile
	{
		private const int jpegQuality = 75; // default of the WinForms (GDI+) and former ImageSharp JPEG encoders

		public static bool IsSupportedExtension(string extension)
		{
			switch (extension.ToLowerInvariant())
			{
				case ".png":
				case ".jpg":
				case ".jpeg":
				case ".bmp":
					return true;
				default:
					return false;
			}
		}

		public static void Save(string fileName, byte[] bgra, int width, int height)
		{
			string extension = Path.GetExtension(fileName).ToLowerInvariant();
			if (extension == ".bmp")
			{
				saveBmp(fileName, bgra, width, height);
				return;
			}
			var format = extension switch
			{
				".png" => SKEncodedImageFormat.Png,
				".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
				_ => throw new NotSupportedException($"Image format '{extension}' is not supported."),
			};
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
			using var image = SKImage.FromPixelCopy(info, bgra, width * 4);
			using var data = image.Encode(format, format == SKEncodedImageFormat.Jpeg ? jpegQuality : 100);
			using var stream = File.Create(fileName);
			data.SaveTo(stream);
		}

		/// <summary>
		/// 24-bit BMP (Skia has no BMP encoder).
		/// </summary>
		private static void saveBmp(string fileName, byte[] bgra, int width, int height)
		{
			int rowBytes = (width * 3 + 3) & ~3; // rows are padded to 4 bytes
			const int headerSize = 14 + 40;
			using var writer = new BinaryWriter(File.Create(fileName));
			// BITMAPFILEHEADER
			writer.Write((byte)'B');
			writer.Write((byte)'M');
			writer.Write(headerSize + rowBytes * height);
			writer.Write(0); // reserved
			writer.Write(headerSize); // offset of the pixels
			// BITMAPINFOHEADER
			writer.Write(40);
			writer.Write(width);
			writer.Write(height); // positive: rows are bottom-up
			writer.Write((short)1); // planes
			writer.Write((short)24); // bits per pixel
			writer.Write(0); // BI_RGB
			writer.Write(rowBytes * height);
			writer.Write(3780); // 96 DPI in pixels per meter
			writer.Write(3780);
			writer.Write(0); // colors used
			writer.Write(0); // important colors

			byte[] row = new byte[rowBytes];
			for (int y = height - 1; y >= 0; y--)
			{
				for (int x = 0; x < width; x++)
				{
					int source = (y * width + x) * 4;
					row[x * 3] = bgra[source];
					row[x * 3 + 1] = bgra[source + 1];
					row[x * 3 + 2] = bgra[source + 2];
				}
				writer.Write(row);
			}
		}
	}
}
