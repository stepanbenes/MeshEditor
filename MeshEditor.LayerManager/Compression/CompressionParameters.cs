using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Compression
{
	public class CompressionParameters
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public CompressionMethod Method { get; set; }

		//public int[] Dimensions { get; set; }
		//public int level { get; set; }
		// Wavelet parameters...
	}
}
