using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Storage;
using Newtonsoft.Json;

namespace MeshEditor.LayerManager.Serialization
{
	public class JsonLayerSerializer : ILayerSerializer
	{
		public string FileExtension => ".json";

		public void Serialize<T>(T layerObject, Stream stream)
		{
			using (StreamWriter writer = new StreamWriter(stream))
			using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
			{
				JsonSerializer jsonSerializer = new JsonSerializer();
				jsonSerializer.Formatting = Formatting.Indented;
				jsonSerializer.Converters.Add(new NotIndentedArrayJsonConverter());
				jsonSerializer.Serialize(jsonWriter, layerObject);
			}
		}

		public T Deserialize<T>(Stream stream)
		{
			using (StreamReader reader = new StreamReader(stream))
			using (JsonTextReader jsonReader = new JsonTextReader(reader))
			{
				JsonSerializer jsonSerializer = new JsonSerializer();
				jsonSerializer.Converters.Add(new KnownTypeConverter());
				return jsonSerializer.Deserialize<T>(jsonReader);
			}
		}
	}
}
