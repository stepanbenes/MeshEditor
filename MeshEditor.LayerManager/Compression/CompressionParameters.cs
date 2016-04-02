using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Compression
{
	[KnownType(typeof(SVDCompressionParameters))]
	public class CompressionParameters
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public virtual CompressionMethod Method => CompressionMethod.None;

		public int Rows { get; set; }
		public int Columns { get; set; }
	}
}
