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
	static class DataImportServiceFactory
	{
		public static IDataImportService Create(IStorageService storageService, IEnumerable<string> filenames)
		{
			Debug.Assert(filenames != null);
			Debug.Assert(filenames.Count() > 0);

			var extension = Path.GetExtension(filenames.First()).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".res": // GiD results 
					return new GiDDataImportService(storageService, filenames);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlDataImportService(storageService, filenames);

				default:
					throw new NotSupportedException();
			}
		}
	}
}
