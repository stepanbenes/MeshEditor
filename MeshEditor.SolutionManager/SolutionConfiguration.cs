using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshEditor.SolutionManager
{
	public class LocalStorageConfiguration
	{
		public string Directory { get; set; }
	}

	public class AzureBlobStorageConfiguration
	{
		public string ConnectionString { get; set; }
		public string ResultsBlobContainerName { get; set; }
		public string LayersBlobContainerName { get; set; }
	}

	public class RestApiConfiguration
	{
		public string Uri { get; set; }
		// resource paths, credentials...
	}
}
