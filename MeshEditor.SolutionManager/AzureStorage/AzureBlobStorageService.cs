using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.SolutionManager.AzureStorage
{
	class AzureBlobStorageService : IStorageService
	{

		public AzureBlobStorageService(string connectionString, string blobContainerName)
		{
			// TODO: create storage account...
		}

		public Stream Load(string record)
		{
			throw new NotImplementedException();
		}

		public Stream Save(string record)
		{
			throw new NotImplementedException();
		}

		public void Delete(string record)
		{
			throw new NotImplementedException();
		}
	}
}
