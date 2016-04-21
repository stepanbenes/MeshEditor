using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;
using MeshEditor.SolutionManager.AzureStorage;
using MeshEditor.SolutionManager.IO;
using MeshEditor.SolutionManager.Logging;

namespace MeshEditor.SolutionManager.Configuration
{
	class ConfigLoader
	{
		ILogger logger;
		string defaultConfigFileDirectory;

		public ConfigLoader(ILogger logger, string defaultConfigFileDirectory = null)
		{
			this.logger = logger;
			this.defaultConfigFileDirectory = defaultConfigFileDirectory;
		}

		public void ReadConfiguration(string configFile, string localFileSystemDefaultDirectory, out ISolutionProvider solutionProvider, out IStorageService meshImportStorage, out IStorageService dataImportStorage, out IStorageService layerSourceStorage, out IStorageService layerDestinationStorage)
		{
			string configFileAbsolutePath;
			if (configFile == null)
			{
				configFileAbsolutePath = Path.Combine(defaultConfigFileDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
			}
			else
			{
				configFileAbsolutePath = (Path.IsPathRooted(configFile)) ? configFile : Path.Combine(defaultConfigFileDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFile);
			}

			if (File.Exists(configFileAbsolutePath))
			{
				Config config = null;
				using (var stream = new FileStream(configFileAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					ISerializationService serializer = new JsonSerializationService();
					config = serializer.Deserialize<Config>(stream);
				}

				solutionProvider = createSolutionProvider(config.SolutionProvider);

				meshImportStorage = createStorageService(config.MeshImportStorage);
				dataImportStorage = createStorageService(config.DataImportStorage);
				layerSourceStorage = createStorageService(config.LayerSourceStorage);
				layerDestinationStorage = createStorageService(config.LayerDestinationStorage);
			}
			else
			{
				solutionProvider = new LocalSolutionProvider(localFileSystemDefaultDirectory);
				meshImportStorage = dataImportStorage = layerSourceStorage = layerDestinationStorage = new LocalFileSystemStorageService(localFileSystemDefaultDirectory);
			}
		}

		private ISolutionProvider createSolutionProvider(SolutionProviderInfo solutionProviderInfo)
		{
			switch (solutionProviderInfo.Type)
			{
				case SolutionProviderType.Local:
					{
						var localSolutionProviderInfo = (LocalSolutionProviderInfo)solutionProviderInfo;
						return new LocalSolutionProvider(localSolutionProviderInfo.Directory ?? Directory.GetCurrentDirectory());
					}
				case SolutionProviderType.RestApi:
					{
						var restApiSolutionProviderInfo = (RestApiSolutionProviderInfo)solutionProviderInfo;
						return new RestApiSolutionProvider(restApiSolutionProviderInfo.BaseUri, logger);
					}
				default:
					throw new NotSupportedException();
			}
		}

		private IStorageService createStorageService(StorageInfo storageInfo)
		{
			switch (storageInfo.Type)
			{
				case StorageType.Local:
					{
						var localStorageInfo = (LocalStorageInfo)storageInfo;
						return new LocalFileSystemStorageService(localStorageInfo.Directory ?? Directory.GetCurrentDirectory());
					}
				case StorageType.AzureBlob:
					{
						var azureBlobStorageInfo = (AzureBlobStorageInfo)storageInfo;
						return new AzureBlobStorageService(azureBlobStorageInfo.ConnectionString, azureBlobStorageInfo.BaseUri, azureBlobStorageInfo.BlobContainerName);
					}
				default:
					throw new NotSupportedException();
			}
		}
	}
}
