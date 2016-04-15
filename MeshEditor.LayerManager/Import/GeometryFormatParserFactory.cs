using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager.Import
{
	public static class GeometryFormatParserFactory
	{
		public static IGeometryImportService Create(IReadStorageService storageService, string recordName)
		{
			Debug.Assert(recordName != null);

			var extension = Path.GetExtension(recordName).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".msh": // GiD mesh 
					return new GiDGeometryFormatParser(storageService, recordName);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlGeometryFormatParser(storageService, recordName);
				
				default:
					throw new NotSupportedException();
			}
		}
	}
}
