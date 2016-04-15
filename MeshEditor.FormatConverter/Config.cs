using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.FormatConverter
{
	enum StorageType
	{
		Local,
		AzureBlob
	}

	[EnumValueTypeSelector(StorageType.Local, typeof(LocalStorageInfo), nameof(Type))]
	[EnumValueTypeSelector(StorageType.AzureBlob, typeof(AzureBlobStorageInfo), nameof(Type))]
	abstract class StorageInfo
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public abstract StorageType Type { get; }
	}

	class LocalStorageInfo : StorageInfo
	{
		public override StorageType Type => StorageType.Local;
		public string Directory { get; set; }
	}

	class AzureBlobStorageInfo : StorageInfo
	{
		public override StorageType Type => StorageType.AzureBlob;
		public string ConnectionString { get; set; }
		public string BlobContainerName { get; set; }
	}

	class Config
	{
		public StorageInfo ImportStorage { get; set; }
		public StorageInfo LayerSourceStorage { get; set; }
		public StorageInfo LayerDestinationStorage { get; set; }
	}
}
