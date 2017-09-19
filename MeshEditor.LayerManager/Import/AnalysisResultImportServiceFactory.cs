using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager.Import
{
	public static class AnalysisResultImportServiceFactory
	{
		public static IAnalysisResultImportService Create(IReadStorageService storageService, AnalysisResult result, string gaussPointsExtrapolationStrategyName)
		{
			GaussPointsExtrapolationStrategy gaussPointsExtrapolationStrategy;
			if (gaussPointsExtrapolationStrategyName == null)
			{
				gaussPointsExtrapolationStrategy = GaussPointsExtrapolationStrategy.Default;
			}
			else if (!Enum.TryParse(gaussPointsExtrapolationStrategyName, ignoreCase: true, result: out gaussPointsExtrapolationStrategy))
			{
				throw new ArgumentException($"Unknown compression method passed as first parameter ({gaussPointsExtrapolationStrategy})", nameof(gaussPointsExtrapolationStrategyName));
			}

			return new AnalysisResultImportService(
				geometryImportService: createGeometryImportService(storageService, result.MeshRecordNames.Single()),
				dataImportService: result.DataRecordNames.Any() ? createDataImportService(storageService, result.TimeStep, result.DataRecordNames, gaussPointsExtrapolationStrategy) : null
			);
		}

		private static IGeometryImportService createGeometryImportService(IReadStorageService storageService, string recordName)
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

		private static IDataImportService createDataImportService(IReadStorageService storageService, decimal? timeStep, IEnumerable<string> recordNames, GaussPointsExtrapolationStrategy gaussPointsExtrapolationStrategy)
		{
			Debug.Assert(recordNames != null);
			Debug.Assert(recordNames.Count() > 0);

			var extension = Path.GetExtension(recordNames.First()).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".res": // GiD results 
					return new GiDDataFormatParser(storageService, recordNames, gaussPointsExtrapolationStrategy);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlDataFormatParser(storageService, timeStep ?? 0m, recordNames);

				default:
					throw new NotSupportedException();
			}
		}
	}
}
