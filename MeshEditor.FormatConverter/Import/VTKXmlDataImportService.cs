using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.FormatConverter.Import
{
	class VTKXmlDataImportService : VTKXmlBase, IDataImportService
	{
		IStorageService storageService;
		IEnumerable<string> filenames;

		public VTKXmlDataImportService(IStorageService storageService, IEnumerable<string> filenames)
		{
			this.storageService = storageService;
			this.filenames = filenames;
		}

		public IEnumerable<DataDescription> ReadData()
		{
			throw new NotImplementedException();
		}
	}
}
