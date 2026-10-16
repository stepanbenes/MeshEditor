using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.Web.Worker
{
	/// <summary>
	/// Connection settings sent by the web app: either the storage account (signed in with a Microsoft account, the token is sent with
	/// every call) or a connection string of a development storage (Azurite).
	/// </summary>
	sealed class StorageSettings
	{
		public string AccountUrl { get; set; }
		public string ConnectionString { get; set; }
		public string SolutionsContainer { get; set; } = "fea-solutions";
		public string LayersContainer { get; set; } = "fea-layers";
		public string ResultsContainer { get; set; } = "fea-results";
		public string UserId { get; set; }
		public string UserName { get; set; }
	}

	/// <summary>
	/// The signed-in user's remote storage: blob clients and the solution controller of the desktop app.
	/// </summary>
	sealed class RemoteSession
	{
		private readonly BlobServiceClient serviceClient;
		private readonly Dictionary<int, BlobRecordStore> layerStores = new Dictionary<int, BlobRecordStore>();

		public RemoteSession(StorageSettings settings, TokenCredential credential)
		{
			Settings = settings ?? throw new ArgumentNullException(nameof(settings));
			if (string.IsNullOrWhiteSpace(settings.UserId))
				throw new ArgumentException("User is not specified", nameof(settings));
			serviceClient = string.IsNullOrWhiteSpace(settings.ConnectionString)
				? new BlobServiceClient(new Uri(settings.AccountUrl), credential)
				: new BlobServiceClient(settings.ConnectionString);
			User = new RemoteUser { Id = settings.UserId, Name = settings.UserName };
			Controller = new AzureBlobSolutionController(SolutionsContainer, LayersContainer, User);
		}

		public StorageSettings Settings { get; }
		public RemoteUser User { get; }
		public AzureBlobSolutionController Controller { get; }

		public BlobContainerClient SolutionsContainer => serviceClient.GetBlobContainerClient(Settings.SolutionsContainer);
		public BlobContainerClient LayersContainer => serviceClient.GetBlobContainerClient(Settings.LayersContainer);
		public BlobContainerClient ResultsContainer => serviceClient.GetBlobContainerClient(Settings.ResultsContainer);

		/// <summary>
		/// Records of the layers of the solution, kept in memory for viewing while the solution is open.
		/// </summary>
		public BlobRecordStore GetViewStore(Solution solution)
		{
			if (!layerStores.TryGetValue(solution.Id, out var store))
			{
				layerStores.Clear(); // one open solution at a time, the records can be large
				store = new BlobRecordStore(LayersContainer, Controller.GetLayerFolder(solution));
				layerStores[solution.Id] = store;
			}
			return store;
		}

		public void ForgetViewStore(int solutionId) => layerStores.Remove(solutionId);

		/// <summary>
		/// Development storage only: creates the containers if they don't exist. Tells what to do when the emulator is not
		/// running or not set up for the web app.
		/// </summary>
		public async Task EnsureContainersAsync(CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(Settings.ConnectionString))
				return;
			// the emulator runs on this computer: when it doesn't answer, the retries would only delay the message by half a minute
			var options = new BlobClientOptions();
			options.Retry.MaxRetries = 0;
			var client = new BlobServiceClient(Settings.ConnectionString, options);
			try
			{
				foreach (string container in new[] { Settings.SolutionsContainer, Settings.LayersContainer, Settings.ResultsContainer })
					await client.GetBlobContainerClient(container).CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException ex) when (ex.Status == 0)
			{
				// the browser doesn't tell a stopped emulator from a missing CORS rule
				throw new InvalidOperationException($"The development storage (Azurite) at {client.Uri} can't be reached. Start Azurite, then run MeshEditor.Web.DevSetup, which allows this site (CORS). See MeshEditor.Web/README.md.", ex);
			}
			catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidHeaderValue" && ex.Message.Contains("API version", StringComparison.Ordinal))
			{
				throw new InvalidOperationException("Azurite doesn't know the storage API version of this app. Start it with --skipApiVersionCheck (see MeshEditor.Web/README.md).", ex);
			}
		}
	}

	/// <summary>
	/// Access token obtained by the web app (MSAL.js), replaced before every call.
	/// </summary>
	sealed class WebAppTokenCredential : TokenCredential
	{
		private AccessToken token;

		public void Set(string accessToken, DateTimeOffset expiresOn) => token = new AccessToken(accessToken, expiresOn);

		public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => check();

		public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => new ValueTask<AccessToken>(check());

		private AccessToken check() => string.IsNullOrEmpty(token.Token)
			? throw new InvalidOperationException("Not signed in.")
			: token;
	}
}
