using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager.Serialization
{
	public interface ISerializationService
	{
		string FileExtension { get; }

		void Serialize<T>(T obj, Stream stream);
		Task SerializeAsync<T>(T obj, Stream stream, CancellationToken cancellationToken);

		T Deserialize<T>(Stream stream);
		Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken);
	}
}
