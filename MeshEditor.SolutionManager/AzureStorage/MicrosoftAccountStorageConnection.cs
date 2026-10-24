using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Storage.Blobs;
using MeshEditor.Common;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace MeshEditor.SolutionManager.AzureStorage
{
	/// <summary>
	/// Access to the remote storage with a Microsoft account (Microsoft Entra ID). Uses a public client app registration,
	/// the sign-in runs in the system web browser (Windows, Linux, macOS) or with a device code (command line, no browser).
	/// The tokens are cached in the secure storage of the operating system (DPAPI on Windows, Keychain on macOS,
	/// Secret Service keyring on Linux; a file readable only by the user when no keyring is available),
	/// so the sign-in is remembered across program runs.
	/// </summary>
	class MicrosoftAccountStorageConnection : IRemoteStorageConnection
	{
		#region Fields, constructor

		static readonly string[] StorageScopes = { "https://storage.azure.com/.default" };

		const string CacheFileName = "msal-token-cache.bin";
		const string KeyChainService = "MeshEditor";
		const string KeyChainAccount = "MicrosoftAccountTokenCache";
		const string LinuxKeyringSchema = "com.mesheditor.tokencache";
		const string LinuxKeyringLabel = "MeshEditor Microsoft account sign-in";

		const string SignInSucceededPage = "<html><head><title>MeshEditor</title></head><body style=\"font-family:sans-serif\"><h3>MeshEditor: you are signed in.</h3><p>You can close this window.</p></body></html>";

		private readonly AzureBlobStorageConfiguration configuration;
		private readonly SemaphoreSlim applicationLock = new SemaphoreSlim(1, 1);
		private IPublicClientApplication application;

		public MicrosoftAccountStorageConnection(AzureBlobStorageConfiguration configuration)
		{
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		#endregion

		#region IRemoteStorageConnection

		public bool IsConfigured => configuration.IsComplete;

		public async Task<RemoteUser> GetSignedInUserAsync(CancellationToken cancellationToken)
		{
			if (!IsConfigured)
				return null;
			var account = (await (await getApplicationAsync().ConfigureAwait(false)).GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();
			return account == null ? null : createUser(account);
		}

		public async Task<RemoteUser> SignInInteractiveAsync(Func<string, Task> showDeviceCodeMessage, CancellationToken cancellationToken)
		{
			var app = await getApplicationAsync().ConfigureAwait(false);
			AuthenticationResult result;
			try
			{
				result = await app.AcquireTokenInteractive(StorageScopes)
					.WithUseEmbeddedWebView(false) // system browser, works on all platforms
					.WithPrompt(Prompt.SelectAccount)
					.WithSystemWebViewOptions(new SystemWebViewOptions { HtmlMessageSuccess = SignInSucceededPage })
					.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			}
			catch (MsalClientException ex) when (ex.ErrorCode == MsalError.LinuxXdgOpen && showDeviceCodeMessage != null)
			{
				// no web browser can be opened (Linux without a desktop session)
				return await SignInWithDeviceCodeAsync(showDeviceCodeMessage, cancellationToken).ConfigureAwait(false);
			}
			return await keepOnlyAccountAsync(app, result.Account).ConfigureAwait(false);
		}

		public async Task<RemoteUser> SignInWithDeviceCodeAsync(Func<string, Task> showDeviceCodeMessage, CancellationToken cancellationToken)
		{
			var app = await getApplicationAsync().ConfigureAwait(false);
			var result = await app.AcquireTokenWithDeviceCode(StorageScopes, deviceCode => showDeviceCodeMessage(deviceCode.Message))
				.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			return await keepOnlyAccountAsync(app, result.Account).ConfigureAwait(false);
		}

		public async Task SignOutAsync()
		{
			if (!IsConfigured)
				return;
			var app = await getApplicationAsync().ConfigureAwait(false);
			foreach (var account in await app.GetAccountsAsync().ConfigureAwait(false))
			{
				await app.RemoveAsync(account).ConfigureAwait(false); // removes the cached tokens of the account
			}
		}

		public BlobServiceClient CreateServiceClient(RemoteUser user)
		{
			if (user == null)
				throw new RemoteSignInRequiredException("Sign in with your Microsoft account to use the remote storage.");
			return new BlobServiceClient(new Uri(configuration.AccountUrl), new MsalTokenCredential(this, user));
		}

		#endregion

		#region Private methods

		private async Task<IPublicClientApplication> getApplicationAsync()
		{
			if (!IsConfigured)
				throw new InvalidOperationException("Remote storage is not configured (AccountUrl, TenantId and ClientId in the AzureBlobStorage section of config.json).");

			await applicationLock.WaitAsync().ConfigureAwait(false);
			try
			{
				if (application == null)
				{
					var app = PublicClientApplicationBuilder.Create(configuration.ClientId)
						.WithAuthority(AzureCloudInstance.AzurePublic, configuration.TenantId)
						.WithRedirectUri("http://localhost") // loopback redirect of the system browser sign-in
						.Build();
					var cacheHelper = await createCacheHelperAsync().ConfigureAwait(false);
					cacheHelper.RegisterCache(app.UserTokenCache);
					application = app;
				}
				return application;
			}
			finally
			{
				applicationLock.Release();
			}
		}

		private static async Task<MsalCacheHelper> createCacheHelperAsync()
		{
			string directory = ConfigurationManager.ConfigurationDirectory;
			Directory.CreateDirectory(directory);

			var properties = new StorageCreationPropertiesBuilder(CacheFileName, directory)
				.WithMacKeyChain(KeyChainService, KeyChainAccount)
				.WithLinuxKeyring(LinuxKeyringSchema, MsalCacheHelper.LinuxKeyRingDefaultCollection, LinuxKeyringLabel,
					new KeyValuePair<string, string>("Application", "MeshEditor"),
					new KeyValuePair<string, string>("Purpose", "MicrosoftAccountTokenCache"))
				.Build();
			var cacheHelper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(false);

			if (OperatingSystem.IsLinux())
			{
				try
				{
					cacheHelper.VerifyPersistence();
				}
				catch (MsalCachePersistenceException)
				{
					// no Secret Service (e.g. headless machine or WSL): use a file readable only by the current user
					var fallbackProperties = new StorageCreationPropertiesBuilder(CacheFileName, directory)
						.WithLinuxUnprotectedFile()
						.Build();
					cacheHelper = await MsalCacheHelper.CreateAsync(fallbackProperties).ConfigureAwait(false);
					string cacheFile = Path.Combine(directory, CacheFileName);
					if (!File.Exists(cacheFile))
						File.WriteAllBytes(cacheFile, Array.Empty<byte>());
					File.SetUnixFileMode(cacheFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
				}
			}
			return cacheHelper;
		}

		/// <summary>
		/// Only one user is signed in: signing in with another account signs the previous one out.
		/// </summary>
		private static async Task<RemoteUser> keepOnlyAccountAsync(IPublicClientApplication app, IAccount signedInAccount)
		{
			foreach (var account in await app.GetAccountsAsync().ConfigureAwait(false))
			{
				if (account.HomeAccountId.Identifier != signedInAccount.HomeAccountId.Identifier)
					await app.RemoveAsync(account).ConfigureAwait(false);
			}
			return createUser(signedInAccount);
		}

		private static RemoteUser createUser(IAccount account) => new RemoteUser
		{
			Id = account.HomeAccountId.ObjectId,
			Name = account.Username,
		};

		private async Task<AccessToken> acquireTokenSilentAsync(RemoteUser user, CancellationToken cancellationToken)
		{
			var app = await getApplicationAsync().ConfigureAwait(false);
			var account = (await app.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault(a => a.HomeAccountId.ObjectId == user.Id);
			if (account == null)
				throw new RemoteSignInRequiredException($"{user} is not signed in. Sign in with your Microsoft account to use the remote storage.");
			try
			{
				var result = await app.AcquireTokenSilent(StorageScopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
				return new AccessToken(result.AccessToken, result.ExpiresOn);
			}
			catch (MsalUiRequiredException ex)
			{
				throw new RemoteSignInRequiredException($"The sign-in of {user} expired. Sign in with your Microsoft account again.", ex);
			}
		}

		#endregion

		#region class MsalTokenCredential

		/// <summary>
		/// Access tokens for Azure Storage of the signed-in user (refreshed silently from the token cache).
		/// </summary>
		private class MsalTokenCredential : TokenCredential
		{
			private readonly MicrosoftAccountStorageConnection connection;
			private readonly RemoteUser user;

			public MsalTokenCredential(MicrosoftAccountStorageConnection connection, RemoteUser user)
			{
				this.connection = connection;
				this.user = user;
			}

			public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
			{
				// synchronous storage calls (layer generation): run on the thread pool to not deadlock on a UI synchronization context
				return Task.Run(() => connection.acquireTokenSilentAsync(user, cancellationToken)).GetAwaiter().GetResult();
			}

			public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
			{
				return await connection.acquireTokenSilentAsync(user, cancellationToken).ConfigureAwait(false);
			}
		}

		#endregion
	}
}
