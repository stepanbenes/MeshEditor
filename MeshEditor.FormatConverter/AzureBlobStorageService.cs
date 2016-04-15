using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.FormatConverter
{
	class AzureBlobStorageService : IStorageService
	{
		public string ConnectionString { get; }
		public string BlobContainerName { get; }

		public AzureBlobStorageService(string connectionString, string blobContainerName)
		{
			ConnectionString = connectionString;
			BlobContainerName = blobContainerName;
		}

		public Stream Load(Uri uri)
		{
			throw new NotImplementedException();
		}

		public Stream Save(Uri uri)
		{
			throw new NotImplementedException();
		}

		public void Delete(Uri uri)
		{
			throw new NotImplementedException();
		}
	}
}
