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
		public static readonly string FileExtension = ".json";

		public void Serialize<T>(T layerObject, string recordName, IStorageService storage)
		{
			using (Stream stream = storage.Save(recordName + FileExtension))
			using (StreamWriter writer = new StreamWriter(stream))
			using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
			{
				JsonSerializer jsonSerializer = new JsonSerializer();
				jsonSerializer.Formatting = Formatting.Indented;
				jsonSerializer.Converters.Add(new NotIndentedArrayJsonConverter());
				jsonSerializer.Serialize(jsonWriter, layerObject);
			}
		}

		public T Deserialize<T>(string recordName, IStorageService storage)
		{
			using (Stream stream = storage.Load(recordName + FileExtension))
			using (StreamReader reader = new StreamReader(stream))
			using (JsonTextReader jsonReader = new JsonTextReader(reader))
			{
				JsonSerializer jsonSerializer = new JsonSerializer();
				return jsonSerializer.Deserialize<T>(jsonReader);
			}
		}
	}
}
