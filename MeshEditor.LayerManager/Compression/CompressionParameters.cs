using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Compression
{
	[EnumValueTypeSelector(CompressionMethod.SVD, typeof(SVDCompressionParameters), enumPropertyName: nameof(Method))]
	public class CompressionParameters
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public virtual CompressionMethod Method => CompressionMethod.None;

		public int Rows { get; set; }
		public int Columns { get; set; }
	}
}
