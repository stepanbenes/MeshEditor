using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MeshEditor.Common;
using Microsoft.Identity.Client.Extensions.Msal;

namespace MeshEditor.SolutionManager.AzureStorage
{
	/// <summary>
	/// Keeps the Azure Storage connection string out of the configuration file and the repository.
	/// The connection string is stored in the secure storage of the operating system:
	/// DPAPI-encrypted file on Windows, Keychain on macOS and the Secret Service (libsecret keyring) on Linux.
	/// When no keyring is available on Linux (headless machine, WSL), it falls back to a file readable only by the current user.
	/// The environment variable <see cref="ConnectionStringEnvironmentVariable"/> takes precedence over the stored value (CI, servers).
	/// </summary>
	public static class AzureStorageCredentialStore
	{
		public const string ConnectionStringEnvironmentVariable = "MESHEDITOR_AZURE_STORAGE_CONNECTION_STRING";

		const string FileName = "azure-storage.bin";
		const string KeyChainService = "MeshEditor";
		const string KeyChainAccount = "AzureStorageConnectionString";
		const string LinuxKeyringSchema = "com.mesheditor.azurestorage";
		const string LinuxKeyringLabel = "MeshEditor Azure Storage connection string";

		static readonly object syncRoot = new object();

		/// <summary>
		/// True if the connection string comes from the environment variable (it can't be changed or cleared by the application).
		/// </summary>
		public static bool IsProvidedByEnvironment => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable));

		/// <summary>
		/// Returns the connection string or null if none is configured.
		/// </summary>
		public static string GetConnectionString()
		{
			string fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
			if (!string.IsNullOrWhiteSpace(fromEnvironment))
				return fromEnvironment.Trim();

			lock (syncRoot)
			{
				byte[] data = createStorage().ReadData();
				if (data == null || data.Length == 0)
					return null;
				string connectionString = Encoding.UTF8.GetString(data);
				return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString;
			}
		}

		public static void SetConnectionString(string connectionString)
		{
			if (string.IsNullOrWhiteSpace(connectionString))
			{
				Clear();
				return;
			}
			lock (syncRoot)
			{
				createStorage().WriteData(Encoding.UTF8.GetBytes(connectionString.Trim()));
				restrictFilePermissions();
			}
		}

		public static void Clear()
		{
			lock (syncRoot)
			{
				createStorage().Clear(ignoreExceptions: true);
			}
		}

		private static Storage createStorage()
		{
			string directory = ConfigurationManager.ConfigurationDirectory;
			Directory.CreateDirectory(directory);

			var properties = new StorageCreationPropertiesBuilder(FileName, directory)
				.WithMacKeyChain(KeyChainService, KeyChainAccount)
				.WithLinuxKeyring(LinuxKeyringSchema, MsalCacheHelper.LinuxKeyRingDefaultCollection, LinuxKeyringLabel,
					new KeyValuePair<string, string>("Application", "MeshEditor"),
					new KeyValuePair<string, string>("Purpose", "AzureStorage"))
				.Build();

			var storage = Storage.Create(properties);
			if (OperatingSystem.IsLinux())
			{
				try
				{
					storage.VerifyPersistence();
				}
				catch (MsalCachePersistenceException)
				{
					// no Secret Service (e.g. headless machine or WSL): use a file readable only by the current user
					var fallbackProperties = new StorageCreationPropertiesBuilder(FileName, directory)
						.WithLinuxUnprotectedFile()
						.Build();
					storage = Storage.Create(fallbackProperties);
				}
			}
			return storage;
		}

		private static void restrictFilePermissions()
		{
			string path = Path.Combine(ConfigurationManager.ConfigurationDirectory, FileName);
			if (!OperatingSystem.IsWindows() && File.Exists(path))
			{
				File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
			}
		}
	}
}
