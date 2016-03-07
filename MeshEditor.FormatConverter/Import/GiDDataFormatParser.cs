using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.FormatConverter.Import
{
	class GiDDataFormatParser : IDataImportService
	{
		IStorageService storageService;
		IEnumerable<string> filenames;

		public GiDDataFormatParser(IStorageService storageService, IEnumerable<string> filenames)
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
