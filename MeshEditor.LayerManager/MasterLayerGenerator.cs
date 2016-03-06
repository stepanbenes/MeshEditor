using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.LayerManager
{
	public class MasterLayerGenerator : LayerGenerator
	{
		public MasterLayerGenerator(
			IStorageService storageService,
			ILayerSerializer layerSerializer = null,
			ICompressionService compressionService = null)
			: base(
				  storageService,
				  layerSerializer,
				  compressionService)
		{ }

		public void Generate(string projectName, IGeometryImportService geometryImportService, IDataImportService dataImportService = null)
		{
			if (geometryImportService == null)
			{
				throw new ArgumentNullException(nameof(geometryImportService));
			}

			GeometryDescription geometry = geometryImportService.ReadGeometry();
			IEnumerable<DataDescription> data = dataImportService?.ReadData() ?? Enumerable.Empty<DataDescription>();

			foreach (var dataComponent in data)
			{

			}

			throw new NotImplementedException();
		}
	}
}
