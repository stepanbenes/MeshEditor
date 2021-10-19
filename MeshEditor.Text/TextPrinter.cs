using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.IO;
using System.Reflection;

namespace MeshEditor.Text
{
	public sealed class TextPrinter
	{
		private static Lazy<TextPrinter> instance = new Lazy<TextPrinter>(() => new TextPrinter());

		public static TextPrinter Instance => instance.Value;

		private readonly int textureId;

		private static readonly float characterAspectRatio = 60f / 82f; // width / height
		private static readonly float characterAspectRatioInverse = 1f / characterAspectRatio;
		private const float betweenLineDistance = 4;

		private const int defaultFontSize = 14;

		private TextPrinter()
		{
			textureId = LoadTexture();
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

			GL.Begin(PrimitiveType.Quads);
		}

		public void End()
		{
			GL.End();

			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.Disable(EnableCap.Texture2D);

			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Modelview);
		}

		public void Print(string text, Vector2 position, System.Drawing.Color color, int fontSize = defaultFontSize)
		{
			var (characterWidth, characterHeight) = GetCharacterSize(fontSize);

			//GL.Color3(1f, 1f, 1f); // white color to blend with texture
			GL.Color3(color);
			PrintLine(text, position.X, position.Y, characterWidth, characterHeight);
		}

		public void PrintLines(string text, Vector2 position, System.Drawing.Color color, int fontSize = defaultFontSize)
		{
			var (characterWidth, characterHeight) = GetCharacterSize(fontSize);

			//GL.Color3(1f, 1f, 1f); // white color to blend with texture
			GL.Color3(color);
			float charPosY = position.Y;
			using var reader = new StringReader(text);
			while (reader.ReadLine() is string line)
			{
				PrintLine(line, position.X, charPosY, characterWidth, characterHeight);
				charPosY += characterHeight + betweenLineDistance;
			}
		}

		public (float width, float height) Measure(string text, int fontSize = defaultFontSize)
		{
			var (characterWidth, characterHeight) = GetCharacterSize(fontSize);
			return (width: text.Length * characterWidth, height: characterHeight);
		}

		public (float width, float height) MeasureLines(string text, int fontSize = defaultFontSize)
		{
			int lineCount = 1;
			int indexOfNewLine = 0;
			int maxLineLength = 0;
			while (true)
			{
				int newIndexOfNewLine = text.IndexOf(Environment.NewLine, indexOfNewLine);
				if (newIndexOfNewLine < 0)
				{
					maxLineLength = Math.Max(maxLineLength, text.Length - indexOfNewLine);
					break;
				}
				lineCount += 1;
				maxLineLength = Math.Max(maxLineLength, newIndexOfNewLine - indexOfNewLine);
				indexOfNewLine = newIndexOfNewLine + Environment.NewLine.Length;
			}

			var (characterWidth, characterHeight) = GetCharacterSize(fontSize);

			return (width: characterWidth * maxLineLength, height: characterHeight * lineCount + betweenLineDistance * (lineCount - 1));
		}

		private int LoadTexture()
		{
			// https://opentk.net/learn/chapter1/5-textures.html

			//Load the image
			using var stream = typeof(TextPrinter).Assembly.GetManifestResourceStream("MeshEditor.Text.Resources.ascii.png");
			using Image<Rgba32> image = Image.Load<Rgba32>(stream/*, new SixLabors.ImageSharp.Formats.Png.PngDecoder()*/);

			//ImageSharp loads from the top-left pixel, whereas OpenGL loads from the bottom-left, causing the texture to be flipped vertically.
			//This will correct that, making the texture display properly.
			//image.Mutate(x => x.Flip(FlipMode.Vertical).ApplyProcessor(new BackgroundRemoverProcessor()));
			//image.SaveAsPng("Resources/ascii_converted.png", new SixLabors.ImageSharp.Formats.Png.PngEncoder { ColorType = SixLabors.ImageSharp.Formats.Png.PngColorType.RgbWithAlpha });

			//Convert ImageSharp's format into a byte array, so we can use it with OpenGL.
			var pixels = new byte[4 * image.Width * image.Height];
			int index = 0;
			for (int y = 0; y < image.Height; y++)
			{
				var row = image.GetPixelRowSpan(y);
				for (int x = 0; x < image.Width; x++)
				{
					pixels[index++] = row[x].R;
					pixels[index++] = row[x].G;
					pixels[index++] = row[x].B;
					pixels[index++] = row[x].A;
				}
			}

			int texID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, texID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			return texID;
		}

		private static void PrintLine(string line, float charPosX, float charPosY, float characterWidth, float characterHeight)
		{
			foreach (char ch in line)
			{
				var (s, t, width, height) = convertCharToTexCoords(ch);
				GL.TexCoord2(s, t);
				GL.Vertex2(charPosX, charPosY + characterHeight);
				GL.TexCoord2(s + width, t);
				GL.Vertex2(charPosX + characterWidth, charPosY + characterHeight);
				GL.TexCoord2(s + width, t + height);
				GL.Vertex2(charPosX + characterWidth, charPosY);
				GL.TexCoord2(s, t + height);
				GL.Vertex2(charPosX, charPosY);

				charPosX += characterWidth;

				static (float s, float t, float width, float height) convertCharToTexCoords(char ch)
				{
					int index = (int)ch;
					if (index > 127) // no
						index = 0;
					int row = (127 - index) / 16;
					int column = index % 16;
					return (column / 16f, row / 8f + 0.01f, characterAspectRatio / 16f, 1 / 8f - 0.01f);
				}
			}
		}

		private static (float characterWidth, float characterHeight) GetCharacterSize(int fontSize)
		{
			return (characterWidth: fontSize * characterAspectRatio, characterHeight: fontSize * characterAspectRatioInverse);
		}
	}
}
