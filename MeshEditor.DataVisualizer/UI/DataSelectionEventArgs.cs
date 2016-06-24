using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.WinUI
{
	public class DataSelectionEventArgs : EventArgs
	{
		public int MeshIndex { get; }
		public int DataIndex { get; }
		public double TimeStep { get; }

		public DataSelectionEventArgs(int meshIndex, int dataIndex, double timeStep)
		{
			MeshIndex = meshIndex;
			DataIndex = dataIndex;
			TimeStep = timeStep;
		}
	}
}
