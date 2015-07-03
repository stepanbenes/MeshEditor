using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// trida urcena pro informovani klienta o zacatku a konci nejake operace
	/// </summary>
	public class LongOpNotifier
	{
		public event EventHandler HasBegun;
		public event EventHandler HasEnd;

		public void Begin()
		{
			if (HasBegun != null)
				HasBegun(this, EventArgs.Empty);
		}

		public void End()
		{
			if (HasEnd != null)
				HasEnd(this, EventArgs.Empty);
		}
	}
}
