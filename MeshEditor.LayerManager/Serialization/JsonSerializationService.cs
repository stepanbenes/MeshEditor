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
	public class JsonSerializationService : ISerializationService
	{
		public string FileExtension => ".json";

		public void Serialize<T>(T obj, Stream stream)
		{
			using (StreamWriter writer = new StreamWriter(stream))
			using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
			{
				JsonSerializer jsonSerializer = new JsonSerializer();
				jsonSerializer.Formatting = Formatting.Indented;
				jsonSerializer.Converters.Add(new NotIndentedArrayJsonConverter());
				jsonSerializer.Serialize(jsonWriter, obj);
			}
		}

		public T Deserialize<T>(Stream stream)
		{
			using (StreamReader reader = new StreamReader(stream))
			using (JsonTextReader jsonReader = new JsonTextReader(reader))
			{
				JsonSerializer jsonSerializer = new JsonSerializer();
				//jsonSerializer.Converters.Add(new KnownTypeConverter());
				jsonSerializer.Converters.Add(new EnumValueTypeSelectorJsonConverter());
				return jsonSerializer.Deserialize<T>(jsonReader);
			}
		}
	}
}
