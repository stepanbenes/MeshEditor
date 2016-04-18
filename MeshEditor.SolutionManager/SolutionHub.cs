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
		IStorageService importStorage, layerSourceStorage, layerDestinationStorage;
		ILogger logger;

		public SolutionHub(ILogger logger = null)
		{
			this.logger = logger;
			ConfigLoader.ReadConfiguration(out solutionProvider, out importStorage, out layerSourceStorage, out layerDestinationStorage);
		}

		#endregion

		#region Commands (SolutionManager's public interface)

		public IEnumerable<ISolutionInfo> EnumerateSolutions()
		{
			return solutionProvider.GetAll();
		}

		public void Import(string projectName, string meshFile, IEnumerable<string> resultFiles)
		{
			const string masterLayerName = "master";
			IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(importStorage, meshFile);
			IDataImportService dataImportService = resultFiles.Any() ? DataFormatParserFactory.Create(importStorage, resultFiles) : null;
			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			var masterLayer = layerGenerator.GenerateMasterLayer(masterLayerName, geometryImportService, dataImportService);
			logNewLayer(masterLayer);
			projectName = projectName ?? Path.GetFileNameWithoutExtension(meshFile);
			var solution = createSolutionFromMasterLayer(masterLayer, projectName);

			solutionProvider.Create(solution);
		}

		public void Filter(ISolutionInfo solutionInfo, string parentLayerIdOrName, string filterTypeName, IEnumerable<string> filterParameters, string layerName)
		{
			FilterType filterType;
			if (!Enum.TryParse(filterTypeName, ignoreCase: true, result: out filterType))
				throw new ArgumentException($"Unknown filter type ({filterTypeName})", nameof(filterTypeName));

			Filter filter = FilterFactory.Create(filterType, filterParameters);

			processLayer(solutionInfo, parentLayerIdOrName,
				layer =>
				{
					var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
					var filterLayer = layerGenerator.GenerateFilterLayer(layer.Id, filter, layerName);
					logNewLayer(filterLayer);
					// convert filter layer to layer record and append it to parent layer's children
					var childLayer = createLayerRecordFromFilterLayer(filterLayer);
					layer.Children = layer.Children.EmptyIfNull().Append(childLayer).ToArray();
				},
				updateSolutionFile: true
			);
		}

		public void Compress(ISolutionInfo solutionInfo, string layerIdOrName, string compressionMethodName, string fieldName, string componentName)
		{
			CompressionMethod method;
			if (!Enum.TryParse(compressionMethodName, ignoreCase: true, result: out method))
				throw new ArgumentException($"Unknown compression method ({compressionMethodName})", nameof(compressionMethodName));

			processLayer(solutionInfo, layerIdOrName,
				layer =>
				{
					var layerGenerator = new LayerGenerator(compressionService: CompressionServiceFactory.Create(method), sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
					var compressedLayer = layerGenerator.CompressLayer(layer.Id, $"time compression ({method})", fieldName, componentName);
					logNewLayer(compressedLayer);
					// convert filter layer to layer record and append it to parent layer's children
					var childLayer = createLayerRecordFromFilterLayer(compressedLayer);
					layer.Children = layer.Children.EmptyIfNull().Append(childLayer).ToArray();
				},
				updateSolutionFile: true
			);
		}

		public void Diff(ISolutionInfo solutionInfo, string layerIdOrName)
		{
			processLayer(solutionInfo, layerIdOrName,
				layer =>
				{
					var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
					var diff = layerGenerator.CreateDiff(layer.Id);
					logger?.LogMessage(diff.ToString());
				},
				updateSolutionFile: false
			);
		}

		#endregion

		#region Helper methods

		private static Solution createSolutionFromMasterLayer(SummaryLayerFile masterLayer, string projectName)
		{
			Solution solution = new Solution
			{
				ProjectName = projectName,
				Layers = new[]
				{
					new Solution.Layer
					{
						Id = masterLayer.Id,
						Name = masterLayer.Name,
						Filter = null,
						Children = null
					}
				}
			};
			return solution;
		}

		private static Solution.Layer createLayerRecordFromFilterLayer(SummaryLayerFile filterLayer)
		{
			var newLayerRecord = new Solution.Layer
			{
				Id = filterLayer.Id,
				Name = filterLayer.Name,
				Filter = filterLayer.Filter,
				Children = null
			};
			return newLayerRecord;
		}

		private void processLayer(ISolutionInfo solutionInfo, string layerIdentifier, Action<Solution.Layer> processLayerOperation, bool updateSolutionFile)
		{
			Solution solution = solutionProvider.Get(solutionInfo.Uri);

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

			// --------------------------
			processLayerOperation(layer);
			// --------------------------

			if (updateSolutionFile)
			{
				solutionProvider.Update(solution);
			}
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
