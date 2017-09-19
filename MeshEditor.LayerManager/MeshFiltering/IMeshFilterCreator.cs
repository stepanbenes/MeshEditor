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
		IEnumerable<(GeometryDescription geometry, List<decimal> timeSteps)> Create(GeometryDescription source, IEnumerable<decimal> timeSteps);
	}
}
