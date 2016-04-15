using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Filters
{
	internal class SurfaceFilter : Filter
	{
		public override FilterType Type => FilterType.Surface;

		public float[] EdgeAngleLimits { get; set; }
	}
}
