using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Text
{
	class BackgroundRemoverProcessor : IImageProcessor
	{
		public IImageProcessor<TPixel> CreatePixelSpecificProcessor<TPixel>(Configuration configuration, Image<TPixel> source, Rectangle sourceRectangle) where TPixel : unmanaged, IPixel<TPixel>
		{
			return new BackgroundRemoverProcessor<TPixel>(source);
		}
	}

	class BackgroundRemoverProcessor<TPixel> : IImageProcessor<TPixel> where TPixel : unmanaged, IPixel<TPixel>
	{
		private readonly Image<TPixel> image;

		public BackgroundRemoverProcessor(Image<TPixel> image)
		{
			this.image = image;
		}

		public void Execute()
		{
			for (int i = 0; i < image.Width; i++)
			{
				for (int j = 0; j < image.Height; j++)
				{
					var pixel = image[i, j];
					Rgba32 rgba = new Rgba32();
					pixel.ToRgba32(ref rgba);
					//Rgba32 rgbaUpdated = new Rgba32(255, 0, 0, 255);
					double luminance = 0.2126 * rgba.R + 0.7152 * rgba.G + 0.0722 * rgba.B;
					Rgba32 rgbaUpdated = new Rgba32(255, 255, 255, (byte)luminance);
					pixel.FromRgba32(rgbaUpdated);
					image[i, j] = pixel;
				}
			}
		}

		public void Dispose()
		{
			// do nothing
		}
	}
}
