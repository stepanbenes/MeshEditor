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
	public static class DataFormatParserFactory
	{
		public static IDataImportService Create(IReadStorageService storageService, IEnumerable<string> recordNames)
		{
			Debug.Assert(recordNames != null);
			Debug.Assert(recordNames.Count() > 0);

			var extension = Path.GetExtension(recordNames.First()).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".res": // GiD results 
					return new GiDDataFormatParser(storageService, recordNames);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlDataFormatParser(storageService, recordNames);

				default:
					throw new NotSupportedException();
			}
		}
	}
}
