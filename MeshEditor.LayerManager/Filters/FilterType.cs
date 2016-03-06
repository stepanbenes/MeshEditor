using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Filters
{
	public enum FilterType
	{
		Surface = 1, // surface of the whole mesh
		Slice = 2, // cross-section
		Clip = 3, // crop
		IsoSurface = 4, // surface with constant data value
	}
}
