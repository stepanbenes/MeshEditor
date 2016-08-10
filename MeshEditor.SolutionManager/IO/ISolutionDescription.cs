using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	public interface ISolutionDescription : ISolutionInfo
	{
		IReadOnlyList<ILayerInfo> Layers { get; }
	}
}
