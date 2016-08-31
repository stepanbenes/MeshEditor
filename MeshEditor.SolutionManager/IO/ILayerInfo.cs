using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	public interface ILayerInfo
	{
		Guid Id { get; }
		string Name { get; }
		string FilterType { get; }
		IEnumerable<ILayerInfo> Children { get; }
	}
}
