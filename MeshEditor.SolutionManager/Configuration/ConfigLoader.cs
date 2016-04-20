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

namespace MeshEditor.SolutionManager.Configuration
{
	class ConfigLoader
	{
		public static void ReadConfiguration(string configFile, out ISolutionProvider solutionProvider, out IStorageService meshImportStorage, out IStorageService dataImportStorage, out IStorageService layerSourceStorage, out IStorageService layerDestinationStorage)
		{
			string configFilePath;
			if (configFile == null)
			{
				configFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
			}
			else
			{
				configFilePath = (Path.IsPathRooted(configFile)) ? configFile : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFile);
			}

			if (File.Exists(configFilePath))
			{
				Config config = null;
				using (var stream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
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
				solutionProvider = new LocalSolutionProvider(Directory.GetCurrentDirectory());
				meshImportStorage = dataImportStorage = layerSourceStorage = layerDestinationStorage = new LocalFileSystemStorageService(Directory.GetCurrentDirectory());
			}
		}

		private static ISolutionProvider createSolutionProvider(SolutionProviderInfo solutionProviderInfo)
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
						return new RestApiSolutionProvider(restApiSolutionProviderInfo.BaseUri);
					}
				default:
					throw new NotSupportedException();
			}
		}

		private static IStorageService createStorageService(StorageInfo storageInfo)
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
