using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshEditor.SolutionManager.Configuration
{
	class ConfigFile
	{
		public StorageInfo ImportStorage { get; set; }
		public StorageInfo LayerSourceStorage { get; set; }
		public StorageInfo LayerDestinationStorage { get; set; }
	}
}
