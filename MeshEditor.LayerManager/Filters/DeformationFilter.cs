using System.Text.Json.Serialization;
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

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string DeformationFieldName { get; set; } = null;

		public double? RelativeScale { get; set; }
	}
}
