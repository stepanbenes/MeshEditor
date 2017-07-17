using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Filters
{
	internal class DeformationFilter : Filter
	{
		public override FilterType Type => FilterType.Deformation;

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string FieldName { get; set; } = null;

		// TODO: add deformation scale?
	}
}
