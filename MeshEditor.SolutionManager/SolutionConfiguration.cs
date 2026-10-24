using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MeshEditor.SolutionManager
{
	public class LocalStorageConfiguration
	{
		public string Directory { get; set; }
	}

	/// <summary>
	/// Remote solution storage in Azure Blob Storage. Users sign in with their Microsoft account (Microsoft Entra ID),
	/// there is no storage key. None of the values is secret, they come with the installation (config.json template).
	/// The remote storage is available only when <see cref="AccountUrl"/>, <see cref="TenantId"/> and <see cref="ClientId"/> are set.
	/// </summary>
	public class AzureBlobStorageConfiguration
	{
		/// <summary>
		/// Blob service endpoint of the storage account, e.g. https://account.blob.core.windows.net
		/// </summary>
		public string AccountUrl { get; set; }

		/// <summary>
		/// Directory (tenant) id of the Microsoft Entra tenant that owns the storage account.
		/// </summary>
		public string TenantId { get; set; }

		/// <summary>
		/// Application (client) id of the public client app registration used for the sign-in.
		/// </summary>
		public string ClientId { get; set; }

		[JsonIgnore]
		public bool IsComplete => !string.IsNullOrWhiteSpace(AccountUrl) && !string.IsNullOrWhiteSpace(TenantId) && !string.IsNullOrWhiteSpace(ClientId);

		public string SolutionsBlobContainerName { get; set; } = "fea-solutions";
		public string LayersBlobContainerName { get; set; } = "fea-layers";
		public string ResultsBlobContainerName { get; set; } = "fea-results";
	}

}
