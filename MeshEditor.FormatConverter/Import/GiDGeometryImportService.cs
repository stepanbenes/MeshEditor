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
		string filename;

		public GiDGeometryImportService(IStorageService storageService, string filename)
		{
			this.storageService = storageService;
			this.filename = filename;
		}

		public GeometryDescription ReadGeometry()
		{
			throw new NotImplementedException();
		}
	}
}
