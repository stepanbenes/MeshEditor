using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshEditor.SolutionManager
{
	class LocalStorageConfiguration
	{
		public string Directory { get; set; }
	}

	class AzureBlobStorageConfiguration
	{
		public string ConnectionString { get; set; }
		public string ResultsBlobContainerName { get; set; }
		public string LayersBlobContainerName { get; set; }
	}

	class RestApiConfiguration
	{
		public string Uri { get; set; }
		// resource paths, credentials...
	}
}
