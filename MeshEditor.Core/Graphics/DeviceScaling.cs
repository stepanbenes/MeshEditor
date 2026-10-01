using System;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// Scales line widths and point sizes (given in logical pixels) to device pixels of the current view.
	/// </summary>
	/// <remarks>
	/// UIs rendering in device pixels on HiDPI displays set <see cref="Factor"/> to the display scaling before drawing.
	/// The default factor 1 keeps the original behaviour.
	/// </remarks>
	public static class DeviceScaling
	{
		private static float factor = 1f;

		public static float Factor
		{
			get { return factor; }
			set { factor = (value > 0f) ? value : 1f; }
		}

		public static void LineWidth(float width)
		{
			GL.LineWidth(width * factor);
		}

		public static void PointSize(float size)
		{
			GL.PointSize(size * factor);
		}
	}
}
