using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Filters
{
	internal class IsoSurfaceFilter : Filter
	{
		public override FilterType Type => FilterType.IsoSurface;

		public string FieldName { get; set; }

		public string ComponentName { get; set; }

		public double Value { get; set; }
	}
}
