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
	/// Records stored as blobs of one Azure Blob Storage container under a folder (the owner's id, or the layer folder of a remote solution).
	/// Record "a/b.json" is blob "{folder}/a/b.json".
	/// </summary>
	class AzureBlobStorageService : IStorageService
	{
		#region Fields, constructor

		private readonly BlobContainerClient container;
		private readonly Lazy<string> folder;

		public AzureBlobStorageService(BlobContainerClient container, string folder)
		{
			this.container = container ?? throw new ArgumentNullException(nameof(container));
			string checkedFolder = checkFolder(folder);
			this.folder = new Lazy<string>(() => checkedFolder);
		}

		/// <summary>
		/// The folder is found on the first access to a record (e.g. read from the remote solution). A failure is not remembered, it is tried again.
		/// </summary>
		public AzureBlobStorageService(BlobContainerClient container, Func<string> getFolder)
		{
			this.container = container ?? throw new ArgumentNullException(nameof(container));
			if (getFolder == null)
				throw new ArgumentNullException(nameof(getFolder));
			folder = new Lazy<string>(() => checkFolder(getFolder()), LazyThreadSafetyMode.PublicationOnly);
		}

		private static string checkFolder(string folder) => string.IsNullOrWhiteSpace(folder) ? throw new ArgumentException("Folder is not specified", nameof(folder)) : folder;

		#endregion

		#region Public methods

		public Stream Load(string record)
		{
			return container.GetBlobClient(ToBlobName(folder.Value, record)).OpenRead();
		}

		public Stream Save(string record)
		{
			return container.GetBlockBlobClient(ToBlobName(folder.Value, record)).OpenWrite(overwrite: true);
		}

		public void Delete(string record)
		{
			container.GetBlobClient(ToBlobName(folder.Value, record)).DeleteIfExists();
		}

		public void DeleteDirectory(string name)
		{
			string prefix = ToBlobName(folder.Value, name).TrimEnd('/') + "/";
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
