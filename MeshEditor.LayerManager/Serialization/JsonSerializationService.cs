using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Storage;
using Newtonsoft.Json;

namespace MeshEditor.LayerManager.Serialization
{
	public class JsonSerializationService : ISerializationService
	{
		public string FileExtension => ".json";

		const int bufferSize = 81920; // https://github.com/dotnet/coreclr/issues/2223

		public void Serialize<T>(T obj, Stream stream)
		{
			using (var streamWriter = new StreamWriter(stream))
			using (var jsonWriter = new JsonTextWriter(streamWriter))
			{
				serializeToJson(jsonWriter, obj);
			}
		}

		public async Task SerializeAsync<T>(T obj, Stream stream, CancellationToken cancellationToken)
		{
			// implementation taken from: https://stackoverflow.com/questions/15631448/json-net-async-when-writing-to-file
			var memoryStream = new MemoryStream(); // stream is disposed insite StreamWriter's Dispose method
			using (var streamWriter = new StreamWriter(memoryStream))
			using (var jsonWriter = new JsonTextWriter(streamWriter))
			{
				serializeToJson(jsonWriter, obj);
				await streamWriter.FlushAsync().ConfigureAwait(false);
				memoryStream.Position = 0;
				await memoryStream.CopyToAsync(stream, bufferSize, cancellationToken).ConfigureAwait(false);
			}
			await stream.FlushAsync().ConfigureAwait(false);
		}

		public T Deserialize<T>(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			using (var jsonReader = new JsonTextReader(reader))
			{
				var jsonSerializer = new JsonSerializer
				{
					Converters = { /*new KnownTypeConverter(),*/ new EnumValueTypeSelectorJsonConverter() }
				};
				return jsonSerializer.Deserialize<T>(jsonReader);
			}
		}

		public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken)
		{
			var memoryStream = new MemoryStream(); // stream is disposed insite StreamReader's Dispose method
			await stream.CopyToAsync(memoryStream, bufferSize, cancellationToken).ConfigureAwait(false);
			memoryStream.Position = 0;
			return Deserialize<T>(memoryStream);
		}

		private static void serializeToJson(JsonWriter jsonWriter, object obj)
		{
			var jsonSerializer = new JsonSerializer
			{
				Formatting = Formatting.Indented,
				Converters = { new NotIndentedArrayJsonConverter() }
			};
			jsonSerializer.Serialize(jsonWriter, obj);
		}
	}
}
