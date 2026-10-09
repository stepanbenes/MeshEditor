using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MeshEditor.Text
{
	/// <summary>
	/// Draws text into the current OpenGL context. Glyphs are rasterized from an embedded TrueType font (Inter)
	/// at the exact device pixel size, so the text is drawn texel-to-pixel with proportional character widths.
	/// </summary>
	public sealed class TextPrinter
	{
		private static Lazy<TextPrinter> instance = new Lazy<TextPrinter>(() => new TextPrinter());

		public static TextPrinter Instance => instance.Value;

		private const float betweenLineDistance = 4;

		private const int defaultFontSize = 14; // TODO: make adjustable in settings

		/// <summary>
		/// Scale of the text (font sizes are in logical pixels); set to the display scaling when rendering in device pixels.
		/// </summary>
		public static float Scale { get; set; } = 1f;

		private readonly FontFamily fontFamily;
		private readonly Dictionary<float, GlyphAtlas> atlases = new Dictionary<float, GlyphAtlas>();
		private GlyphAtlas batchAtlas; // atlas of the open GL.Begin(Quads) batch (strings between Begin and End are drawn in one batch)

		private TextPrinter()
		{
			var fontCollection = new FontCollection();
			using var stream = typeof(TextPrinter).Assembly.GetManifestResourceStream("MeshEditor.Text.Resources.Inter-Medium.ttf");
			fontFamily = fontCollection.Add(stream);
		}

		/// <summary>
		/// Starts printing. Between Begin and End no other GL calls may be made (the quads of all strings are emitted in one GL.Begin/End).
		/// </summary>
		public void Begin()
		{
			batchAtlas = null;
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
		}

		public void End()
		{
			endBatch();
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.Disable(EnableCap.Texture2D);

			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Modelview);
		}

		public void Print(string text, Vector2 position, System.Drawing.Color color, int fontSize = defaultFontSize)
		{
			var atlas = beginBatch(fontSize);
			GL.Color3(color.R, color.G, color.B);
			atlas.PrintLine(text, position.X, position.Y);
		}

		public void PrintLines(string text, Vector2 position, System.Drawing.Color color, int fontSize = defaultFontSize)
		{
			var atlas = beginBatch(fontSize);
			GL.Color3(color.R, color.G, color.B);
			float charPosY = position.Y;
			using var reader = new StringReader(text);
			while (reader.ReadLine() is string line)
			{
				atlas.PrintLine(line, position.X, charPosY);
				charPosY += atlas.LineHeight + betweenLineDistance * Scale;
			}
		}

		/// <summary>
		/// Continues the open batch if it uses the atlas of the font size, otherwise binds the atlas and opens a new one.
		/// </summary>
		private GlyphAtlas beginBatch(int fontSize)
		{
			var atlas = GetAtlas(fontSize);
			if (atlas != batchAtlas)
			{
				endBatch();
				atlas.Bind(); // texture binding is not allowed inside GL.Begin/End
				GL.Begin(PrimitiveType.Quads);
				batchAtlas = atlas;
			}
			return atlas;
		}

		private void endBatch()
		{
			if (batchAtlas == null)
				return;
			GL.End();
			batchAtlas = null;
		}

		public (float width, float height) Measure(string text, int fontSize = defaultFontSize)
		{
			var atlas = GetAtlas(fontSize);
			return (width: atlas.MeasureLine(text), height: atlas.LineHeight);
		}

		public (float width, float height) MeasureLines(string text, int fontSize = defaultFontSize)
		{
			var atlas = GetAtlas(fontSize);
			int lineCount = 0;
			float width = 0;
			using var reader = new StringReader(text);
			while (reader.ReadLine() is string line)
			{
				lineCount += 1;
				width = Math.Max(width, atlas.MeasureLine(line));
			}
			lineCount = Math.Max(lineCount, 1);
			return (width: width, height: atlas.LineHeight * lineCount + betweenLineDistance * Scale * (lineCount - 1));
		}

		private GlyphAtlas GetAtlas(int fontSize)
		{
			float pixelSize = MathF.Round(fontSize * Scale * 4f) / 4f;
			if (!atlases.TryGetValue(pixelSize, out var atlas))
			{
				atlas = new GlyphAtlas(fontFamily.CreateFont(pixelSize, FontStyle.Regular));
				atlases.Add(pixelSize, atlas);
			}
			return atlas;
		}

		/// <summary>
		/// Glyphs of one font size rendered into a single texture.
		/// </summary>
		private sealed class GlyphAtlas
		{
			private const int atlasWidth = 512;
			private const int padding = 2; // room for glyph parts outside of the advance box
			private const char fallbackCharacter = '?';

			private readonly Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>();
			private Image<Rgba32> image; // released after upload to the texture
			private readonly int textureHeight;
			private int textureId;

			public float LineHeight { get; }

			private readonly struct Glyph
			{
				public Glyph(float advance, int x, int y, int width, int height)
				{
					Advance = advance;
					X = x;
					Y = y;
					Width = width;
					Height = height;
				}

				public float Advance { get; }
				public int X { get; }
				public int Y { get; }
				public int Width { get; }
				public int Height { get; }
			}

			public GlyphAtlas(Font font)
			{
				var metrics = font.FontMetrics.HorizontalMetrics;
				float unitsToPixels = font.Size / font.FontMetrics.UnitsPerEm;
				LineHeight = MathF.Ceiling((metrics.Ascender - metrics.Descender) * unitsToPixels);
				int cellHeight = (int)LineHeight + 2 * padding;

				// printable ASCII, Latin-1 Supplement and Latin Extended-A (e.g. Czech characters)
				var characters = new List<char>();
				for (char ch = ' '; ch <= '~'; ch++)
					characters.Add(ch);
				for (char ch = ' '; ch <= 'ſ'; ch++)
					characters.Add(ch);

				var textOptions = new TextOptions(font);
				var layout = new List<(char ch, float advance, int x, int y, int width)>();
				int cellX = 0, cellY = 0;
				foreach (char ch in characters)
				{
					string s = ch.ToString();
					if (!font.TryGetGlyphs(new SixLabors.Fonts.Unicode.CodePoint(ch), out _))
						continue;
					float advance = TextMeasurer.MeasureAdvance(s, textOptions).Width;
					int cellWidth = (int)MathF.Ceiling(advance) + 2 * padding;
					if (cellX + cellWidth > atlasWidth)
					{
						cellX = 0;
						cellY += cellHeight;
					}
					layout.Add((ch, advance, cellX, cellY, cellWidth));
					cellX += cellWidth;
				}

				textureHeight = cellY + cellHeight;
				image = new Image<Rgba32>(atlasWidth, textureHeight, new Rgba32(255, 255, 255, 0));
				image.Mutate(context =>
				{
					foreach (var (ch, advance, x, y, width) in layout)
					{
						if (char.IsWhiteSpace(ch))
							continue;
						var options = new RichTextOptions(font) { Origin = new System.Numerics.Vector2(x + padding, y + padding) };
						context.DrawText(options, ch.ToString(), SixLabors.ImageSharp.Color.White);
					}
				});

				foreach (var (ch, advance, x, y, width) in layout)
					glyphs.Add(ch, new Glyph(advance, x, y, width, cellHeight));
			}

			public void Bind()
			{
				if (textureId == 0)
					textureId = UploadTexture();
				GL.BindTexture(TextureTarget.Texture2D, textureId);
			}

			public float MeasureLine(string line)
			{
				float width = 0;
				foreach (char ch in line)
					width += GetGlyph(ch).Advance;
				return width;
			}

			/// <summary>
			/// Emits quads of the line, must be called between GL.Begin(Quads) and GL.End() with the atlas bound.
			/// </summary>
			public void PrintLine(string line, float charPosX, float charPosY)
			{
				// snap to whole pixels so that texels map exactly to pixels
				float originX = MathF.Round(charPosX);
				float top = MathF.Round(charPosY) - padding;
				float penX = 0;
				foreach (char ch in line)
				{
					var glyph = GetGlyph(ch);
					if (!char.IsWhiteSpace(ch))
					{
						float left = originX + MathF.Round(penX) - padding;
						float s0 = glyph.X / (float)atlasWidth, s1 = (glyph.X + glyph.Width) / (float)atlasWidth;
						float t0 = glyph.Y / (float)textureHeight, t1 = (glyph.Y + glyph.Height) / (float)textureHeight;
						GL.TexCoord2(s0, t1);
						GL.Vertex2(left, top + glyph.Height);
						GL.TexCoord2(s1, t1);
						GL.Vertex2(left + glyph.Width, top + glyph.Height);
						GL.TexCoord2(s1, t0);
						GL.Vertex2(left + glyph.Width, top);
						GL.TexCoord2(s0, t0);
						GL.Vertex2(left, top);
					}
					penX += glyph.Advance;
				}
			}

			private Glyph GetGlyph(char ch)
			{
				return glyphs.TryGetValue(ch, out var glyph) ? glyph : glyphs[fallbackCharacter];
			}

			private int UploadTexture()
			{
				var pixels = new byte[4 * image.Width * image.Height];
				image.CopyPixelDataTo(pixels);

				int texID = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, texID);
				GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

				// glyphs are drawn 1:1 at whole pixel positions
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

				image.Dispose();
				image = null;
				return texID;
			}
		}
	}
}
