using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Storage;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace MeshEditor.SolutionManager.AzureStorage
{
	class AzureBlobStorageService : IStorageService
	{
		#region Fields, constructor

		private readonly CloudStorageAccount storageAccount;
		private readonly string blobContainerName;

		public AzureBlobStorageService(string connectionString, string blobContainerName)
		{
			storageAccount = CloudStorageAccount.Parse(connectionString);
			this.blobContainerName = blobContainerName;
		}

		#endregion

		#region Public methods

		public Stream Load(string record)
		{
			return downloadFile(storageAccount, blobContainerName, record);
		}

		public Stream Save(string record)
		{
			return uploadFile(storageAccount, blobContainerName, record);
		}

		public void Delete(string record)
		{
			deleteFileFromBlobStorage(storageAccount, blobContainerName, record);
		}

		public void DeleteDirectory(string name)
		{
			var blobClient = storageAccount.CreateCloudBlobClient();
			var container = blobClient.GetContainerReference(blobContainerName);
			List<IListBlobItem> blobsToDelete = new List<IListBlobItem>();

			CloudBlobDirectory blobDirectory = container.GetDirectoryReference(name);
			BlobContinuationToken continuationToken = null;
			do
			{
				var listingResult = blobDirectory.ListBlobsSegmented(continuationToken);
				continuationToken = listingResult.ContinuationToken;
				blobsToDelete.AddRange(listingResult.Results);
			} while (continuationToken != null);

			foreach (CloudBlob cloudBlob in blobsToDelete)
			{
				cloudBlob.DeleteIfExists();
			}
		}

		#endregion

		#region Private methods

		private static Stream downloadFile(CloudStorageAccount storageAccount, string blobContainerName, string blobName)
		{
			// Create the blob client and reference the container
			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
			CloudBlobContainer container = blobClient.GetContainerReference(blobContainerName);

			CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

			return blockBlob.OpenRead();
		}

		private static Stream uploadFile(CloudStorageAccount storageAccount, string blobContainerName, string blobName)
		{
			// Create the blob client and reference the container
			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
			CloudBlobContainer container = blobClient.GetContainerReference(blobContainerName);

			// Upload file to Blob Storage
			CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
			//blockBlob.Properties.ContentType = formFile.ContentType;
			return blockBlob.OpenWrite();
			// Convert to be HTTP based URI (default storage path is HTTPS)
			//var uriBuilder = new UriBuilder(blockBlob.Uri);
			//uriBuilder.Port = -1;
			//uriBuilder.Scheme = "http";
			//string fullPath = uriBuilder.ToString();
			//return fullPath;
		}

		private static bool deleteFileFromBlobStorage(CloudStorageAccount storageAccount, string blobContainerName, string blobName)
		{
			// Create the blob client and reference the container
			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
			CloudBlobContainer container = blobClient.GetContainerReference(blobContainerName);
			// Retrieve reference to a blob
			CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
			// Delete the blob
			return blockBlob.DeleteIfExists(); // TODO: make async
		}

		#endregion
	}
}
