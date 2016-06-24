using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.WinUI
{
	public class LayerSelectionEventArgs : EventArgs
	{
		public Guid? LayerId { get; }

		public LayerSelectionEventArgs(Guid? layerId)
		{
			LayerId = layerId;
		}
	}
}
