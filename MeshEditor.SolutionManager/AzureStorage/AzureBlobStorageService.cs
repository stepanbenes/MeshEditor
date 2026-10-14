using System;
using System.IO;
using System.Linq;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.SolutionManager.AzureStorage
{
	/// <summary>
	/// Records stored as blobs of one Azure Blob Storage container. Record "a/b.json" is blob "a/b.json".
	/// </summary>
	class AzureBlobStorageService : IStorageService
	{
		#region Fields, constructor

		private readonly BlobContainerClient container;

		public AzureBlobStorageService(BlobContainerClient container)
		{
			this.container = container ?? throw new ArgumentNullException(nameof(container));
		}

		#endregion

		#region Public methods

		public Stream Load(string record)
		{
			return container.GetBlobClient(ToBlobName(record)).OpenRead();
		}

		public Stream Save(string record)
		{
			container.CreateIfNotExists();
			return container.GetBlockBlobClient(ToBlobName(record)).OpenWrite(overwrite: true);
		}

		public void Delete(string record)
		{
			container.GetBlobClient(ToBlobName(record)).DeleteIfExists();
		}

		public void DeleteDirectory(string name)
		{
			if (!container.Exists())
				return;

			string prefix = ToBlobName(name).TrimEnd('/') + "/";
			foreach (var blob in container.GetBlobs(BlobTraits.None, BlobStates.None, prefix, CancellationToken.None).ToList())
			{
				container.GetBlobClient(blob.Name).DeleteIfExists();
			}
		}

		#endregion

		#region Internal static methods

		/// <summary>
		/// Blob names always use forward slashes (record names of local solutions may contain backslashes on Windows).
		/// </summary>
		internal static string ToBlobName(string record) => record.Replace('\\', '/').TrimStart('/');

		#endregion
	}
}
