using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.MeshFiltering
{
	interface IMeshFilterCreator
	{
		IList<(GeometryDescription geometry, List<double> timeSteps)> Create(GeometryDescription source, IEnumerable<double> timeSteps);
	}
}
