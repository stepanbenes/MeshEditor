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

namespace MeshEditor.SolutionManager.Configuration
{
	class ConfigLoader
	{
		public static void ReadConfiguration(out IStorageService importStorage, out IStorageService layerSourceStorage, out IStorageService layerDestinationStorage)
		{
			string configFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
			if (File.Exists(configFilename))
			{
				ConfigFile config = null;
				using (var stream = new FileStream(configFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					ISerializationService serializer = new JsonSerializationService();
					config = serializer.Deserialize<ConfigFile>(stream);
				}

				importStorage = createStorageService(config.ImportStorage);
				layerSourceStorage = createStorageService(config.LayerSourceStorage);
				layerDestinationStorage = createStorageService(config.LayerDestinationStorage);
			}
			else
			{
				importStorage = layerSourceStorage = layerDestinationStorage = new LocalFileSystemStorageService(Directory.GetCurrentDirectory());
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
						return new AzureBlobStorageService(azureBlobStorageInfo.ConnectionString, azureBlobStorageInfo.BlobContainerName);
					}
				default:
					throw new NotSupportedException();
			}
		}
	}
}
