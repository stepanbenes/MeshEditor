using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// delegat oznamujici o skutecnosti, ze sit musi byt prekreslena
	/// </summary>
	public delegate void MeshNeedRefreshEventHandler(object sender, MeshNeedRefreshEventArgs ea);

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
}
