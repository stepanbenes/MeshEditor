using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshEditor.SolutionManager
{
	class SolutionConfiguration
	{
		public static SolutionConfiguration CreateDefault() => new SolutionConfiguration
																{
																	LocalStorage = new LocalStorageConfigPatameters(),
																	AzureBlobStorage = new AzureBlobStorageConfigParameters(),
																	RestApi = new RestApiConfigParameters()
																};

		public LocalStorageConfigPatameters LocalStorage { get; set; }
		public AzureBlobStorageConfigParameters AzureBlobStorage { get; set; }
		public RestApiConfigParameters RestApi { get; set; }
	}

	class LocalStorageConfigPatameters
	{
		public string Directory { get; set; }
	}

	class AzureBlobStorageConfigParameters
	{
		public string ConnectionString { get; set; }
		public string MeshesBlobContainerName { get; set; }
		public string ResultsBlobContainerName { get; set; }
		public string LayersBlobContainerName { get; set; }
	}

	class RestApiConfigParameters
	{
		public string Uri { get; set; }
		// resource paths, credentials...
	}
}
