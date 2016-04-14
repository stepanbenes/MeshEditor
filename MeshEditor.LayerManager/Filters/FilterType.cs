using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Filters
{
	public enum FilterType
	{
		Surface, // surface of the mesh
		Slice, // cross-section
		Clip, // crop
		IsoSurface, // surface with constant data value
		StreamLines,
		AttributeSelection, // e.g. elements with specific property
		TimeCompression,
	}
}
