using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager
{
	public static class DataFormatParserFactory
	{
		public static IDataImportService Create(IStorageService storageService, IEnumerable<Uri> uris)
		{
			Debug.Assert(uris != null);
			Debug.Assert(uris.Count() > 0);

			var extension = Path.GetExtension(uris.First().LocalPath).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".res": // GiD results 
					return new GiDDataFormatParser(storageService, uris);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlDataFormatParser(storageService, uris);

				default:
					throw new NotSupportedException();
			}
		}
	}
}
