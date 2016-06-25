using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.DataVisualizer;

namespace MeshEditor.WinUI
{
	public class DataSelectionEventArgs : EventArgs
	{
		public DataSelection DataSelection { get; }

		public DataSelectionEventArgs(DataSelection dataSelection)
		{
			DataSelection = dataSelection;
		}
	}
}
