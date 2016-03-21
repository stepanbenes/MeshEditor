using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Data
{
	public class CompressionDescriptor
	{
		public int Level { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public DataArrayType DataType { get; set; }
		public int[] Dimensions { get; set; }
		public int DataOffset { get; set; }
		public int DataLength { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public double? DefaultDataValue { get; set; }
	}
}
