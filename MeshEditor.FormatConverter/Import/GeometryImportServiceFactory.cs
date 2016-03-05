using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.FormatConverter.Import
{
	static class GeometryImportServiceFactory
	{
		public static IGeometryImportService Create(IStorageService storageService, string filename)
		{
			Debug.Assert(filename != null);

			var extension = Path.GetExtension(filename).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".msh": // GiD mesh 
					return new GiDGeometryImportService(storageService, filename);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlGeometryImportService(storageService, filename);
				
				default:
					throw new NotSupportedException();
			}
		}
	}
}
