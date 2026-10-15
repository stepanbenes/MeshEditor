using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace MeshEditor.LayerManager.Filters
{
	internal class TimeCompressionFilter : Filter
	{
		public override FilterType Type => FilterType.TimeCompression;

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string FieldName { get; set; } = null;
	}
}
