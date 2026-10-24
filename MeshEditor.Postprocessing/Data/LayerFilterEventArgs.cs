using MeshEditor.LayerManager.Filters;
using MeshEditor.SolutionManager.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.DataVisualizer.Data
{
	public class LayerFilterEventArgs : LayerSelectionEventArgs
	{
		public FilterType FilterType { get; }
		public LayerFilterEventArgs(ILayerInfo layer, FilterType filterType)
			: base(layer) => FilterType = filterType;
	}
}
