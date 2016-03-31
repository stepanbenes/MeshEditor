using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Encoding
{
	public class EncodingParameters
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public DataArrayType DataType { get; set; }

		public int OriginalLength { get; set; }

		public int Offset { get; set; }

		public int Length { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string DefaultValue { get; set; }
	}
}
