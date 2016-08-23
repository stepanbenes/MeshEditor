using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.DataVisualizer;

namespace MeshEditor.DataVisualizer.Data
{
	public class DataSelectionEventArgs : EventArgs
	{
		public Guid LayerId { get; }
		public string LayerName { get; }
		public DataSelection DataSelection { get; }

		public DataSelectionEventArgs(Guid layerId, string layerName, DataSelection dataSelection)
		{
			LayerId = layerId;
			LayerName = layerName;
			DataSelection = dataSelection;
		}
	}
}
