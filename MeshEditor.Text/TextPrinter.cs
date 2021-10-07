using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace MeshEditor.Text
{
	public sealed class TextPrinter
	{
		private static Lazy<TextPrinter> instance = new Lazy<TextPrinter>(() => new TextPrinter());

		public static TextPrinter Instance => instance.Value;

		private readonly int textureId;

		private static readonly float characterWidth = 14f;
		private static readonly float characterHeight = 14f;

		private TextPrinter()
		{
			textureId = LoadTexture();
		}

		private int LoadTexture()
		{
			// https://opentk.net/learn/chapter1/5-textures.html

			string path = "Resources/ascii.png";

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

		public void Print(string text, System.Drawing.Color color, Vector2 position)
		{
			GL.Begin(PrimitiveType.Quads);
			{
				GL.Color3(1f, 1f, 1f); // white color to blend with texture
				//GL.Color3(color);
				float charPosX = position.X;
				foreach (char ch in text)
				{
					// TODO: wrong!!!!
					var (s, t, width, height) = convertCharPositionToTexCoords(ch);
					GL.TexCoord2(s, t);
					GL.Vertex2(charPosX, position.Y + characterHeight);
					GL.TexCoord2(s + width, t);
					GL.Vertex2(charPosX + characterWidth, position.Y + characterHeight);
					GL.TexCoord2(s + width, t + height);
					GL.Vertex2(charPosX + characterWidth, position.Y);
					GL.TexCoord2(s, t + height);
					GL.Vertex2(charPosX, position.Y);

					charPosX += characterWidth;

					static (float s, float t, float width, float height) convertCharPositionToTexCoords(char ch)
					{
						int index = (int)ch;
						int row = (255 - index) / 16;
						int column = index % 16;
						return (column / 16f, row / 16f, 16f / 256f, 16f / 256f);
					}
				}
			}
			GL.End();
		}

		public (float width, float height) Measure(string text)
		{
			return (width: (text?.Length ?? 0) * characterWidth, height: characterHeight);
		}
	}
}
