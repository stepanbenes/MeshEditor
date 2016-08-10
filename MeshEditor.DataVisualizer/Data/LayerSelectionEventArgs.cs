using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.DataVisualizer.Data
{
	public class LayerSelectionEventArgs : EventArgs
	{
		public ILayerInfo Layer { get; }

		public LayerSelectionEventArgs(ILayerInfo layer)
		{
			Layer = layer;
		}
	}
}
