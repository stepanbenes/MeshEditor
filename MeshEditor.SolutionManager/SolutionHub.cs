using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Common;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;
using MeshEditor.SolutionManager.Configuration;
using MeshEditor.SolutionManager.IO;
using MeshEditor.SolutionManager.Logging;

namespace MeshEditor.SolutionManager
{
	public class SolutionHub
	{
		#region Fields, constructor

		ISolutionProvider solutionProvider;
		IStorageService meshImportStorage, dataImportStorage, layerSourceStorage, layerDestinationStorage;
		ILogger logger;

		public SolutionHub(string configFile = null, ILogger logger = null)
		{
			this.logger = logger;
			new ConfigLoader(logger).ReadConfiguration(configFile, out solutionProvider, out meshImportStorage, out dataImportStorage, out layerSourceStorage, out layerDestinationStorage);
		}

		#endregion

		#region Commands (SolutionManager's public interface)

		public IEnumerable<ISolutionInfo> EnumerateSolutions()
		{
			return solutionProvider.GetAll();
		}

		public IEnumerable<ILayerInfo> EnumerateLayersOfSolution(int solutionId)
		{
			return solutionProvider.Get(solutionId).Layers;
		}

		public void Import(int solutionId, string projectName, string meshFile, IEnumerable<string> resultFiles)
		{
			const string masterLayerName = "master";
			IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(meshImportStorage, meshFile);
			IDataImportService dataImportService = resultFiles.Any() ? DataFormatParserFactory.Create(dataImportStorage, resultFiles) : null;
			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			var masterLayer = layerGenerator.GenerateMasterLayer(masterLayerName, geometryImportService, dataImportService);
			logNewLayer(masterLayer);
			projectName = projectName ?? Path.GetFileNameWithoutExtension(meshFile);

			var solution = new Solution { Id = solutionId, ProjectName = projectName };
			solutionProvider.CreateNew(solution);
			solutionProvider.AddLayer(solution, parentLayer: null, newLayer: createLayerRecordLayerSummaryFile(masterLayer));
		}

		public void Filter(int solutionId, string parentLayerIdOrName, string filterTypeName, IEnumerable<string> filterParameters, string layerName)
		{
			FilterType filterType;
			if (!Enum.TryParse(filterTypeName, ignoreCase: true, result: out filterType))
				throw new ArgumentException($"Unknown filter type ({filterTypeName})", nameof(filterTypeName));

			Filter filter = FilterFactory.Create(filterType, filterParameters);
			Solution solution = solutionProvider.Get(solutionId);

			var parentLayer = findLayer(solution, parentLayerIdOrName);

			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			var filterLayer = layerGenerator.GenerateFilterLayer(parentLayer.Id, filter, layerName);
			logNewLayer(filterLayer);
			// convert filter layer to layer record and append it to parent layer's children
			var childLayer = createLayerRecordLayerSummaryFile(filterLayer);

			solutionProvider.AddLayer(solution, parentLayer, childLayer);
		}

		public void Compress(int solutionId, string layerIdOrName, string compressionMethodName, string fieldName, string componentName)
		{
			CompressionMethod method;
			if (!Enum.TryParse(compressionMethodName, ignoreCase: true, result: out method))
				throw new ArgumentException($"Unknown compression method ({compressionMethodName})", nameof(compressionMethodName));

			Solution solution = solutionProvider.Get(solutionId);

			var parentLayer = findLayer(solution, layerIdOrName);

			var layerGenerator = new LayerGenerator(compressionService: CompressionServiceFactory.Create(method), sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			var compressedLayer = layerGenerator.CompressLayer(parentLayer.Id, $"time compression ({method})", fieldName, componentName);
			logNewLayer(compressedLayer);
			// convert filter layer to layer record and append it to parent layer's children
			var childLayer = createLayerRecordLayerSummaryFile(compressedLayer);

			solutionProvider.AddLayer(solution, parentLayer, childLayer);
		}

		public void Diff(int solutionId, string layerIdOrName)
		{
			Solution solution = solutionProvider.Get(solutionId);
			var layer = findLayer(solution, layerIdOrName);

			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			var diff = layerGenerator.CreateDiff(layer.Id);
			logger?.LogMessage(diff.ToString());
		}

		#endregion

		#region Helper methods

		private static Solution.Layer createLayerRecordLayerSummaryFile(SummaryLayerFile layerSummary)
		{
			var newLayerRecord = new Solution.Layer
			{
				Id = layerSummary.Id,
				Name = layerSummary.Name,
				FilterType = layerSummary.Filter?.Type.ToString(),
				Children = null
			};
			return newLayerRecord;
		}

		private Solution.Layer findLayer(Solution solution, string layerIdentifier)
		{
			// find layer according to either provided layer guid or layer name
			Solution.Layer layer;
			Guid guid;
			if (Guid.TryParse(layerIdentifier, out guid))
			{
				layer = findLayer(solution.Layers, l => l.Id == guid);
			}
			else
			{
				layer = findLayer(solution.Layers, l => string.Equals(l.Name, layerIdentifier, StringComparison.InvariantCultureIgnoreCase));
			}

			if (layer == null)
			{
				throw new Exception($"Layer '{layerIdentifier}' not found.");
			}

			return layer;
		}

		private static Solution.Layer findLayer(IEnumerable<Solution.Layer> layers, Func<Solution.Layer, bool> predicate)
		{
			Debug.Assert(layers != null);
			foreach (var layer in layers)
			{
				if (predicate(layer))
					return layer;
			}
			foreach (var layer in layers)
			{
				var hit = findLayer(layer.Children, predicate);
				if (hit != null)
					return hit;
			}
			return null;
		}

		private IProgress<OperationState> createProgressReporter()
		{
			return new Progress<OperationState>
				(
					state => logger?.LogMessage(state.State)
				);
		}

		private void logNewLayer(SummaryLayerFile layerSummary)
		{
			logger?.LogMessage($"layer name: {layerSummary.Name}, layer id: {layerSummary.Id}");
		}

		#endregion
	}
}
