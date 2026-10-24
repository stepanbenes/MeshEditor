using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using System.Text.Json.Serialization;
using MeshEditor.Common.Serialization;

namespace MeshEditor.LayerManager.Encoding
{
	public class EncodingParameters
	{
		[JsonConverter(typeof(StringEnumJsonConverter))]
		public DataArrayType DataType { get; set; }

		public int OriginalLength { get; set; }

		public int Offset { get; set; }

		public int Length { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string DefaultValue { get; set; }
	}
}
