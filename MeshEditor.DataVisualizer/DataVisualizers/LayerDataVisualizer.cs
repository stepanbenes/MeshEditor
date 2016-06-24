using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.DataVisualizer
{
	internal class LayerDataVisualizer : DataVisualizerBase
	{
		public LayerDataVisualizer(Guid layerId, int meshIndex)
		{
			LayerId = layerId;
			MeshIndex = meshIndex;
		}

		public Guid LayerId { get; }

		public int MeshIndex { get; }
	}
}
