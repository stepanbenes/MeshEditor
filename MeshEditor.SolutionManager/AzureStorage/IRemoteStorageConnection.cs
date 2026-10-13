using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace MeshEditor.SolutionManager.AzureStorage
{
	/// <summary>
	/// Access to the remote storage on behalf of the signed-in user (tests replace the Microsoft account sign-in).
	/// </summary>
	interface IRemoteStorageConnection
	{
		/// <summary>
		/// The remote storage is set up in the configuration.
		/// </summary>
		bool IsConfigured { get; }

		/// <summary>
		/// User signed in earlier (the sign-in is remembered across program runs) or null.
		/// </summary>
		Task<RemoteUser> GetSignedInUserAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Sign-in in the system web browser. When no browser can be opened (e.g. Linux without a desktop),
		/// falls back to the device code flow: <paramref name="showDeviceCodeMessage"/> tells the user where to enter the code.
		/// </summary>
		Task<RemoteUser> SignInInteractiveAsync(Func<string, Task> showDeviceCodeMessage, CancellationToken cancellationToken);

		/// <summary>
		/// Sign-in with a code entered on another device or in a browser (command line).
		/// </summary>
		Task<RemoteUser> SignInWithDeviceCodeAsync(Func<string, Task> showDeviceCodeMessage, CancellationToken cancellationToken);

		Task SignOutAsync();

		/// <summary>
		/// Blob service client authenticated as <paramref name="user"/>.
		/// </summary>
		BlobServiceClient CreateServiceClient(RemoteUser user);
	}
}
