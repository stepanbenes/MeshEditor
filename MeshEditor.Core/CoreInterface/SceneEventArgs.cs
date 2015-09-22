using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using MeshEditor.Data;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// argument udalosti oznamujici, ze sit musi byt prekreslena
	/// </summary>
	public class MeshNeedRefreshEventArgs : EventArgs
	{
		private string meshToRefresh;

		public string MeshToRefresh
		{
			get { return meshToRefresh; }
		}

		public MeshNeedRefreshEventArgs(string meshToRefresh)
		{
			this.meshToRefresh = meshToRefresh;
		}
	}

	public class ScreenshotNeededEventArgs : EventArgs
	{
		private Rectangle screenshotWindow;

		public Rectangle ScreenshotWindow
		{
			get	{ return screenshotWindow; }
		}

		public ScreenshotNeededEventArgs(Rectangle screenshotWindow)
		{
			this.screenshotWindow = screenshotWindow;
		}
	}
}
