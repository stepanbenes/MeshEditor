using System;
using System.Collections.Generic;
using System.Linq;
using MeshEditor.LayerManager.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.SolutionManager.Configuration
{
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
