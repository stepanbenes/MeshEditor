using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Encoding;
using MeshEditor.LayerManager.Serialization;
using System.Text.Json.Serialization;
using MeshEditor.Common.Serialization;

namespace MeshEditor.LayerManager.Data
{
	public class DataFile : IDataDescription
	{
		public Guid LayerId { get; set; }

		public int Index { get; set; }
		public int MeshIndex { get; set; }


		public string FieldName { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string ComponentName { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public decimal[] TimeSteps { get; set; }

		[JsonConverter(typeof(StringEnumJsonConverter))]
		public DataLocationType Location { get; set; }

		public CompressionParameters Compression { get; set; }

		public EncodingParameters Encoding { get; set; }

		/// <summary>
		/// double array data in Base64 string format
		/// </summary>
		public string Data { get; set; }
	}
}
