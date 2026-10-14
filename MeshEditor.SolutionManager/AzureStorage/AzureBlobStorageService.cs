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
	/// Records stored as blobs of one Azure Blob Storage container under a folder (the owner's id).
	/// Record "a/b.json" is blob "{folder}/a/b.json".
	/// </summary>
	class AzureBlobStorageService : IStorageService
	{
		#region Fields, constructor

		private readonly BlobContainerClient container;
		private readonly string folder;

		public AzureBlobStorageService(BlobContainerClient container, string folder)
		{
			this.container = container ?? throw new ArgumentNullException(nameof(container));
			if (string.IsNullOrWhiteSpace(folder))
				throw new ArgumentException("Folder is not specified", nameof(folder));
			this.folder = folder;
		}

		#endregion

		#region Public methods

		public Stream Load(string record)
		{
			return container.GetBlobClient(ToBlobName(folder, record)).OpenRead();
		}

		public Stream Save(string record)
		{
			return container.GetBlockBlobClient(ToBlobName(folder, record)).OpenWrite(overwrite: true);
		}

		public void Delete(string record)
		{
			container.GetBlobClient(ToBlobName(folder, record)).DeleteIfExists();
		}

		public void DeleteDirectory(string name)
		{
			string prefix = ToBlobName(folder, name).TrimEnd('/') + "/";
			foreach (var blob in container.GetBlobs(BlobTraits.None, BlobStates.None, prefix, CancellationToken.None).ToList())
			{
				container.GetBlobClient(blob.Name).DeleteIfExists();
			}
		}

		#endregion

		#region Internal static methods

		/// <summary>
		/// Blob name of the record in the folder. Blob names always use forward slashes (record names of local solutions may contain backslashes on Windows).
		/// </summary>
		internal static string ToBlobName(string folder, string record) => folder + "/" + record.Replace('\\', '/').TrimStart('/');

		#endregion
	}
}
