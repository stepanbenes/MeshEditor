using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.WinUI
{
	public class LayerEventArgs : EventArgs
	{
		public Guid? LayerId { get; }

		public LayerEventArgs(Guid? layerId)
		{
			LayerId = layerId;
		}
	}
}
