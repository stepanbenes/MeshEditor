using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.CoreInterface
{

	/// <summary>
	/// trida urcena pro informovani klienta o zacatku a konci nejake operace
	/// </summary>
	public class LongOpNotifier
	{
		public event EventHandler HasBegun;
		public event EventHandler HasEnded;

		public event MeshIOEventHandler ProgressChanged;

		public bool IsCancelled
		{
			get;
			private set;
		}

		public bool IsRunning
		{
			get;
			private set;
		}

		public void Begin()
		{
			IsCancelled = false;
			IsRunning = true;
			var handler = HasBegun;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		public void End()
		{
			IsRunning = false;
			var handler = HasEnded;
			if (handler != null)
				handler(this, EventArgs.Empty);
			//IsCancelled = false;
		}

		public void Cancel()
		{
			IsCancelled = true;
		}

		public void ReportProgress(int percentDone, string taskName, string operationName = null)
		{
			var handler = ProgressChanged;
			if (handler != null)
				handler(this, new MeshIOEventArgs(percentDone, taskName, operationName));
		}
	}
}
