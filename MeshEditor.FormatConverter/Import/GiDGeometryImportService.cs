using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.FormatConverter.Import
{
	class GiDGeometryImportService : IGeometryImportService
	{
		IStorageService storageService;
		IEnumerable<string> filenames;

		public GiDGeometryImportService(IStorageService storageService, IEnumerable<string> filenames)
		{
			this.storageService = storageService;
			this.filenames = filenames;
		}

		public GeometryDescription ReadGeometry()
		{
			throw new NotImplementedException();
		}
	}
}
