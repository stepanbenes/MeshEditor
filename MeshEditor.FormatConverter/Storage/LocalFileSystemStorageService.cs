using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.FormatConverter.Storage
{
	class LocalFileSystemStorageService : IStorageService
	{
		string directoryName;

		public LocalFileSystemStorageService(string directoryName)
		{
			this.directoryName = directoryName;
		}

		public Stream Load(string filename)
		{
			return new FileStream(Path.Combine(directoryName, filename), FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public void Save(Stream stream, string filename)
		{
			using (var fileStream = new FileStream(Path.Combine(directoryName, filename), FileMode.OpenOrCreate/**/, FileAccess.Write, FileShare.None))
			{
				stream.CopyTo(fileStream);
			}
		}
	}
}
