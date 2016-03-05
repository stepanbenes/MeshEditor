using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.LayerManager
{
	public class FilterLayerGenerator : LayerGenerator
	{
		public FilterLayerGenerator(
			IStorageService storageService,
			ILayerSerializer layerSerializer = null,
			ICompressionService compressionService = null)
			: base(
				  storageService,
				  layerSerializer,
				  compressionService)
		{ }

		public void GenerateFrom(Guid parentLayer, FilterDescriptor filter)
		{
			throw new NotImplementedException();
		}

		public void AppendData(Guid layer, IDataImportService dataImportService)
		{
			throw new NotImplementedException();
		}
	}
}
