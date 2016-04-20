using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshEditor.SolutionManager.Configuration
{
	class Config
	{
		public SolutionProviderInfo SolutionProvider { get; set; }

		public StorageInfo MeshImportStorage { get; set; }
		public StorageInfo DataImportStorage { get; set; }
		public StorageInfo LayerSourceStorage { get; set; }
		public StorageInfo LayerDestinationStorage { get; set; }
	}
}
