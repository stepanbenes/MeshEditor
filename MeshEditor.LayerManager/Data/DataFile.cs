using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Encoding;
using MeshEditor.LayerManager.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Data
{
	public class DataFile : IDataDescription
	{
		public Guid LayerId { get; set; }

		public int DataIndex { get; set; }
		public int MeshIndex { get; set; }


		public string FieldName { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string ComponentName { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public double[] TimeSteps { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public DataLocationType Location { get; set; }

		public CompressionParameters Compression { get; set; }

		public EncodingParameters Encoding { get; set; }

		/// <summary>
		/// double array data in Base64 string format
		/// </summary>
		public string Data { get; set; }
	}
}
