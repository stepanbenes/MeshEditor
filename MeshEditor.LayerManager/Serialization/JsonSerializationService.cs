using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.Common.Serialization;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager.Serialization
{
	public class JsonSerializationService : ISerializationService
	{
		public string FileExtension => ".json";

		const int bufferSize = 81920; // https://github.com/dotnet/coreclr/issues/2223

		private static readonly JsonSerializerOptions serializeOptions = JsonOptions.Create(writeIndented: true, new NotIndentedArrayJsonConverter(), new EnumValueTypeSelectorJsonConverter());
		private static readonly JsonSerializerOptions deserializeOptions = JsonOptions.Create(writeIndented: false, new EnumValueTypeSelectorJsonConverter());

		public void Serialize<T>(T obj, Stream stream)
		{
			using (var streamWriter = new StreamWriter(stream))
			{
				streamWriter.Write(serializeToJson(obj));
			}
		}

		public async Task SerializeAsync<T>(T obj, Stream stream, CancellationToken cancellationToken)
		{
			var memoryStream = new MemoryStream(); // stream is disposed insite StreamWriter's Dispose method
			using (var streamWriter = new StreamWriter(memoryStream))
			{
				streamWriter.Write(serializeToJson(obj));
				await streamWriter.FlushAsync().ConfigureAwait(false);
				memoryStream.Position = 0;
				await memoryStream.CopyToAsync(stream, bufferSize, cancellationToken).ConfigureAwait(false);
			}
			await stream.FlushAsync().ConfigureAwait(false);
		}

		public T Deserialize<T>(Stream stream)
		{
			using (var reader = new StreamReader(stream)) // detects the encoding from the byte order mark
			{
				string json = reader.ReadToEnd();
				if (string.IsNullOrWhiteSpace(json))
					return default(T); // as Newtonsoft.Json
				return JsonSerializer.Deserialize<T>(json, deserializeOptions);
			}
		}

		public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken)
		{
			var memoryStream = new MemoryStream(); // stream is disposed insite StreamReader's Dispose method
			await stream.CopyToAsync(memoryStream, bufferSize, cancellationToken).ConfigureAwait(false);
			memoryStream.Position = 0;
			return Deserialize<T>(memoryStream);
		}

		private static string serializeToJson(object obj)
		{
			// serialized as its runtime type, as by Newtonsoft.Json
			return JsonSerializer.Serialize(obj, obj?.GetType() ?? typeof(object), serializeOptions);
		}
	}
}
