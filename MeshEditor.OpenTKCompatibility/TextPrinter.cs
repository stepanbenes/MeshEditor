using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace MeshEditor.OpenTKCompatibility
{
	public class TextPrinter
	{
		private OpenTK.Graphics.TextPrinter openTK_TextPrinter;
		private static readonly OpenTK.Graphics.TextPrinterOptions textPrinterOptions = OpenTK.Graphics.TextPrinterOptions.NoCache; /* !!! */

		public TextPrinter()
		{
			openTK_TextPrinter = new OpenTK.Graphics.TextPrinter(OpenTK.Graphics.TextQuality.Low);
		}

		public void Begin()
		{
			openTK_TextPrinter.Begin();
		}

		public void End()
		{
			openTK_TextPrinter.End();
		}

		public void Print(string text, Font font, Color color, RectangleF rect)
		{
			openTK_TextPrinter.Print(text, font, color, rect, textPrinterOptions);
		}
	}
}
