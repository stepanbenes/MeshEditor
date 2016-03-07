using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.FormatConverter.Import
{
	static class GeometryFormatParserFactory
	{
		public static IGeometryImportService Create(IStorageService storageService, string filename)
		{
			Debug.Assert(filename != null);

			var extension = Path.GetExtension(filename).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".msh": // GiD mesh 
					return new GiDGeometryFormatParser(storageService, filename);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlGeometryFormatParser(storageService, filename);
				
				default:
					throw new NotSupportedException();
			}
		}
	}
}
