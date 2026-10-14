using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshEditor.SolutionManager
{
	public class LocalStorageConfiguration
	{
		public string Directory { get; set; }
	}

	/// <summary>
	/// Containers of the remote solution storage. The connection string (with the storage key) is not part of the configuration,
	/// it is kept in the secure storage of the operating system (see AzureStorage.AzureStorageCredentialStore).
	/// </summary>
	public class AzureBlobStorageConfiguration
	{
		public string SolutionsBlobContainerName { get; set; } = "fea-solutions";
		public string LayersBlobContainerName { get; set; } = "fea-layers";
		public string ResultsBlobContainerName { get; set; } = "fea-results";
	}

}
