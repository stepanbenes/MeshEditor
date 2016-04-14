using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MeshEditor.LayerManager.Filters
{
	internal class TimeCompressionFilter : FilterBase
	{
		public override FilterType Type => FilterType.TimeCompression;

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string FieldName { get; set; } = null;

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string ComponentName { get; set; } = null;
	}
}
