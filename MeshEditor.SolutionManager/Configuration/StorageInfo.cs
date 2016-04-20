using System;
using System.Collections.Generic;
using System.Linq;
using MeshEditor.LayerManager.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.SolutionManager.Configuration
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
		public string BaseUri { get; set; }
	}
}
