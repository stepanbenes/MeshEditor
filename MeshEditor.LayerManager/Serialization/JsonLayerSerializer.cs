using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MeshEditor.LayerManager.Serialization
{
	class JsonLayerSerializer : ILayerSerializer
	{
		public string FileExtension => ".json";

		public T Deserialize<T>(Stream layerStream)
		{
			using (StreamReader reader = new StreamReader(layerStream))
			using (JsonTextReader jsonReader = new JsonTextReader(reader))
			{
				JsonSerializer jsonSerializer = new JsonSerializer();
				return jsonSerializer.Deserialize<T>(jsonReader);
			}
		}

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
	}
}
