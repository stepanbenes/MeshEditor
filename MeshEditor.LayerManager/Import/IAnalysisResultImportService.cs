using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Import
{
	public interface IAnalysisResultImportService : IGeometryImportService, IDataImportService
	{
	}

	internal class AnalysisResultImportService : IAnalysisResultImportService
	{
		IGeometryImportService geometryImportService;
		IDataImportService dataImportService;

		public AnalysisResultImportService(IGeometryImportService geometryImportService, IDataImportService dataImportService)
		{
			Debug.Assert(geometryImportService != null);
			this.geometryImportService = geometryImportService;
			this.dataImportService = dataImportService;
		}

		public GeometryDescription ReadGeometry(out IReadOnlyList<AttributeDescription> attributes) => geometryImportService.ReadGeometry(out attributes);
		public IEnumerable<FieldDataDescription> ReadData(GeometryDescription correspondingGeometry) => dataImportService?.ReadData(correspondingGeometry) ?? Enumerable.Empty<FieldDataDescription>();
	}
}
