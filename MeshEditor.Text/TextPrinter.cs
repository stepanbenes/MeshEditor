using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Text
{
	public sealed class TextPrinter
	{
		private static Lazy<TextPrinter> instance = new Lazy<TextPrinter>(() => new TextPrinter());

		public static TextPrinter Instance => instance.Value;

		private readonly int textureId;

		private TextPrinter()
		{
			textureId = LoadTexture();
		}

		private int LoadTexture()
		{
			// https://opentk.net/learn/chapter1/5-textures.html

			string path = "Resources/duck.png";

			//Load the image
			Image<Rgba32> image = Image.Load<Rgba32>(path);

			//ImageSharp loads from the top-left pixel, whereas OpenGL loads from the bottom-left, causing the texture to be flipped vertically.
			//This will correct that, making the texture display properly.
			image.Mutate(x => x.Flip(FlipMode.Vertical));

			//Convert ImageSharp's format into a byte array, so we can use it with OpenGL.
			var pixels = new List<byte>(4 * image.Width * image.Height); // TODO: make array

			for (int y = 0; y < image.Height; y++)
			{
				var row = image.GetPixelRowSpan(y);

				for (int x = 0; x < image.Width; x++)
				{
					pixels.Add(row[x].R);
					pixels.Add(row[x].G);
					pixels.Add(row[x].B);
					pixels.Add(row[x].A);
				}
			}

			int texID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, texID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.ToArray());
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			return texID;
		}

		public void Begin()
		{
			int[] viewport = new int[4];
			GL.GetInteger(GetPName.Viewport, viewport);

			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Ortho(0, viewport[2], viewport[3], 0, 0, 1);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();

			GL.Disable(EnableCap.Lighting);
			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.Texture2D);

			GL.BindTexture(TextureTarget.Texture2D, textureId);
		}

		public void End()
		{
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.Disable(EnableCap.Texture2D);

			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Modelview);
		}

		public void Print(string text, System.Drawing.Color color, System.Drawing.RectangleF rect)
		{
			const float sizeX = 40f;
			const float sizeY = 40f;

			GL.Begin(PrimitiveType.Quads);
			{
				//GL.Color3(1f, 1f, 1f); // white color to blend with texture
				GL.Color3(color);
				GL.TexCoord2(0f, 0f);
				GL.Vertex2(rect.X, rect.Y + sizeY);
				GL.TexCoord2(1f, 0f);
				GL.Vertex2(rect.X + sizeX, rect.Y + sizeY);
				GL.TexCoord2(1f, 1f);
				GL.Vertex2(rect.X + sizeX, rect.Y);
				GL.TexCoord2(0f, 1f);
				GL.Vertex2(rect.X, rect.Y);
			}
			GL.End();
		}

		public System.Drawing.RectangleF Measure(string text, System.Drawing.RectangleF rect)
		{
			return System.Drawing.RectangleF.Empty;
		}
	}
}
