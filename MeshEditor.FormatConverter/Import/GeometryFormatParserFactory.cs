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
		public static IGeometryImportService Create(IStorageService storageService, Uri uri)
		{
			Debug.Assert(uri != null);

			var extension = Path.GetExtension(uri.LocalPath).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".msh": // GiD mesh 
					return new GiDGeometryFormatParser(storageService, uri);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlGeometryFormatParser(storageService, uri);
				
				default:
					throw new NotSupportedException();
			}
		}
	}
}
