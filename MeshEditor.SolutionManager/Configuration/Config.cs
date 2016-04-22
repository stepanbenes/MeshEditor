using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshEditor.SolutionManager.Configuration
{
	class Config
	{
		public LocalStorageConfigPatameters LocalStorage { get; set; }
		public AzureBlobStorageConfigParameters AzureBlobStorage { get; set; }
		public RestApiConfigParameters RestApi { get; set; }
	}
}
