using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace MeshEditor.OpenTKCompatibility
{
	public class TextPrinter
	{
		public TextPrinter()
		{
		}

		public void Begin()
		{
		}

		public void End()
		{
		}

		public void Print(string text, Font font, Color color, RectangleF rect)
		{
		}

		public RectangleF Measure(string text, Font font, RectangleF rect)
		{
			return RectangleF.Empty;
		}
	}
}
