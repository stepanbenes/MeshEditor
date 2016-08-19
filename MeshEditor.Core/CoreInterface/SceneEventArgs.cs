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
		public string MeshToRefresh { get; }
		public bool SkipSender { get; }

		public MeshNeedRefreshEventArgs(string meshToRefresh, bool skipSender = false)
		{
			MeshToRefresh = meshToRefresh;
			SkipSender = skipSender;
		}
	}

	public class ScreenshotNeededEventArgs : EventArgs
	{
		public Rectangle ScreenshotWindow { get; }

		public ScreenshotNeededEventArgs(Rectangle screenshotWindow)
		{
			ScreenshotWindow = screenshotWindow;
		}
	}
}
