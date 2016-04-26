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
	[EnumValueTypeSelector(CompressionMethod.Transparent, typeof(TransparentCompressionParameters), enumPropertyName: nameof(Method))]
	[EnumValueTypeSelector(CompressionMethod.SVD, typeof(SVDCompressionParameters), enumPropertyName: nameof(Method))]
	[EnumValueTypeSelector(CompressionMethod.WT, typeof(WaveletCompressionParameters), enumPropertyName: nameof(Method))]
	public abstract class CompressionParameters
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public abstract CompressionMethod Method { get; }

		public int Rows { get; set; }
		public int Columns { get; set; }
	}
}
