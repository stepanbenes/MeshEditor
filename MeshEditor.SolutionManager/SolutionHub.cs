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
using MeshEditor.SolutionManager.AzureStorage;
using MeshEditor.SolutionManager.Configuration;
using MeshEditor.SolutionManager.IO;
using MeshEditor.SolutionManager.Logging;

namespace MeshEditor.SolutionManager
{
	public class SolutionHub
	{
		#region Public static methods

		public static SolutionHub CreateLocal(string solutionFileName, ILogger logger = null)
		{
			Debug.Assert(solutionFileName != null);
			string solutionDirectory = Path.GetDirectoryName(solutionFileName);
			var solutionController = new LocalSolutionController(solutionDirectory);
			var solutionInfo = solutionController.LoadSolutionFromFileName(Path.GetFileName(solutionFileName));

			IStorageService localStorage = new LocalFileSystemStorageService(solutionDirectory);
			return new SolutionHub(
				solutionId: solutionInfo.Id,
				solutionController: solutionController,
				meshImportStorage: localStorage,
				dataImportStorage: localStorage,
				layerSourceStorage: localStorage,
				layerDestinationStorage: localStorage,
				logger: logger
			);
		}

		public static SolutionHub CreateLocal(int solutionId, ILogger logger = null)
		{
			Config config = new ConfigLoader().ReadConfiguration();
			var solutionDirectory = config.LocalStorage.Directory ?? Directory.GetCurrentDirectory();
			IStorageService localStorage = new LocalFileSystemStorageService(solutionDirectory);
			return new SolutionHub(
				solutionId: solutionId,
				solutionController: new LocalSolutionController(solutionDirectory),
				meshImportStorage: localStorage,
				dataImportStorage: localStorage,
				layerSourceStorage: localStorage,
				layerDestinationStorage: localStorage,
				logger: logger
			);
		}

		public static SolutionHub CreateRemote(int solutionId, ILogger logger = null)
		{
			Config config = new ConfigLoader().ReadConfiguration();
			return new SolutionHub(
				solutionId: solutionId,
				solutionController: new RestApiSolutionController(config.RestApi.Uri, logger),
				meshImportStorage: new AzureBlobStorageService(config.AzureBlobStorage.ConnectionString, config.AzureBlobStorage.MeshesBlobContainerName),
				dataImportStorage: new AzureBlobStorageService(config.AzureBlobStorage.ConnectionString, config.AzureBlobStorage.ResultsBlobContainerName),
				layerSourceStorage: new AzureBlobStorageService(config.AzureBlobStorage.ConnectionString, config.AzureBlobStorage.LayersBlobContainerName),
				layerDestinationStorage: new AzureBlobStorageService(config.AzureBlobStorage.ConnectionString, config.AzureBlobStorage.LayersBlobContainerName),
				logger: logger
			);
		}

		public static IEnumerable<ISolutionInfo> EnumerateAllLocalSolutions(ILogger logger = null)
		{
			Config config = new ConfigLoader().ReadConfiguration();
			var solutionDirectory = config.LocalStorage.Directory ?? Directory.GetCurrentDirectory();
			var solutionController = new LocalSolutionController(solutionDirectory);
			return solutionController.GetAll();
		}

		public static IEnumerable<ISolutionInfo> EnumerateAllRemoteSolutions(ILogger logger = null)
		{
			Config config = new ConfigLoader().ReadConfiguration();
			var solutionController = new RestApiSolutionController(config.RestApi.Uri, logger);
			return solutionController.GetAll();
		}

		#endregion

		#region Fields, Constructors

		int solutionId;
		ISolutionController solutionController;
		IStorageService meshImportStorage, dataImportStorage, layerSourceStorage, layerDestinationStorage;
		ILogger logger;

		private SolutionHub(int solutionId, ISolutionController solutionController, IStorageService meshImportStorage, IStorageService dataImportStorage, IStorageService layerSourceStorage, IStorageService layerDestinationStorage, ILogger logger = null)
		{
			this.solutionId = solutionId;
			this.solutionController = solutionController;
			this.meshImportStorage = meshImportStorage;
			this.dataImportStorage = dataImportStorage;
			this.layerSourceStorage = layerSourceStorage;
			this.layerDestinationStorage = layerDestinationStorage;
			this.logger = logger;
		}

		#endregion

		#region Commands (SolutionManager's public interface)

		public IEnumerable<ILayerInfo> EnumerateAllLayers()
		{
			return from layer in solutionController.Get(solutionId).Layers select layer;
		}

		public void Create(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames, string projectName)
		{
			var analysisResults = composeAnalysisResults(analysisResultGroupLengths, analysisResultRecordNames);
			Solution solution = solutionController.CreateNew(solutionId, analysisResults, projectName);
		}

		public void Import(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames)
		{
			const string masterLayerName = "master";

			Solution solution = solutionController.Get(solutionId);

			var analysisResults = composeAnalysisResults(analysisResultGroupLengths, analysisResultRecordNames);

			var analysisResultImportServices = analysisResults.Select(result => AnalysisResultImportServiceFactory.Create(meshImportStorage, dataImportStorage, result));

			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			var masterLayer = layerGenerator.GenerateMasterLayer(masterLayerName, analysisResultImportServices);
			logNewLayer(masterLayer);
			
			solutionController.AddLayer(solution, parentLayer: null, newLayer: createLayerRecordLayerSummaryFile(masterLayer));
		}

		public void Filter(string parentLayerIdOrName, string filterTypeName, IEnumerable<string> filterParameters, string layerName)
		{
			FilterType filterType;
			if (!Enum.TryParse(filterTypeName, ignoreCase: true, result: out filterType))
				throw new ArgumentException($"Unknown filter type ({filterTypeName})", nameof(filterTypeName));

			Filter filter = FilterFactory.Create(filterType, filterParameters);
			Solution solution = solutionController.Get(solutionId);

			var parentLayer = findLayer(solution, parentLayerIdOrName);

			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			var filterLayer = layerGenerator.GenerateFilterLayer(parentLayer.Id, filter, layerName);
			logNewLayer(filterLayer);
			// convert filter layer to layer record and append it to parent layer's children
			var childLayer = createLayerRecordLayerSummaryFile(filterLayer);

			solutionController.AddLayer(solution, parentLayer, childLayer);
		}

		public void Compress(string parentLayerIdOrName, string compressionMethodName, IEnumerable<double> keyTimeSteps, string fieldName, string componentName, IEnumerable<string> compressionParameters, string compressedLayerName = null)
		{
			CompressionMethod method;
			if (!Enum.TryParse(compressionMethodName, ignoreCase: true, result: out method))
				throw new ArgumentException($"Unknown compression method ({compressionMethodName})", nameof(compressionMethodName));

			Solution solution = solutionController.Get(solutionId);

			var parentLayer = findLayer(solution, parentLayerIdOrName);

			var layerGenerator = new LayerGenerator(compressionService: CompressionServiceFactory.Create(method, compressionParameters), sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			var compressedLayer = layerGenerator.CompressLayer(parentLayer.Id, keyTimeSteps, compressedLayerName ?? $"{method} {string.Join(" ", compressionParameters)}".Trim(), fieldName, componentName);
			logNewLayer(compressedLayer);
			// convert filter layer to layer record and append it to parent layer's children
			var childLayer = createLayerRecordLayerSummaryFile(compressedLayer);

			solutionController.AddLayer(solution, parentLayer, childLayer);
		}

		public void Delete(string layerIdOrName, bool deleteAll, bool updateSolution)
		{
			Solution solution = solutionController.Get(solutionId);
			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter());
			IEnumerable<Solution.Layer> layersToDelete = deleteAll ? solution.Layers : Enumerable.Repeat(findLayer(solution, layerIdOrName), 1);
			foreach (var rootLayer in layersToDelete)
			{
				foreach (var layer in traverseLayerTreePostOrder(rootLayer))
				{
					if (updateSolution)
					{
						solutionController.DeleteLayer(solution, layer);
					}
					layerGenerator.DeleteAllLayerFiles(layer.Id);
				}
			}
		}

		#endregion

		#region Private helper methods

		private static IEnumerable<Solution.Layer> traverseLayerTreePostOrder(Solution.Layer root)
		{
			Stack<Solution.Layer> result = new Stack<Solution.Layer>();
			Stack<Solution.Layer> children = new Stack<Solution.Layer>();
			children.Push(root);
			while (children.Count > 0)
			{
				var layer = children.Pop();
				result.Push(layer);
				if (layer.Children != null)
				{
					foreach (var child in layer.Children)
					{
						children.Push(child);
					}
				}
			}
			return result;
		}

		private static List<AnalysisResult> composeAnalysisResults(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames)
		{
			var analysisResults = new List<AnalysisResult>();
			int offset = 0;
			foreach (int groupLength in analysisResultGroupLengths)
			{
				var resultGroup = analysisResultRecordNames.Skip(offset).Take(groupLength).ToList();
				analysisResults.Add(new AnalysisResult(resultGroup.Take(1).ToArray(), resultGroup.Skip(1).ToArray()));
				offset += groupLength;
			}

			return analysisResults;
		}

		private static Solution.Layer createLayerRecordLayerSummaryFile(SummaryFile layerSummary)
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
			if (layerIdentifier == null)
				throw new ArgumentNullException(nameof(layerIdentifier));

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
			if (layers != null)
			{
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

		private void logNewLayer(SummaryFile layerSummary)
		{
			logger?.LogMessage($"layer name: {layerSummary.Name}, layer id: {layerSummary.Id}");
		}

		#endregion
	}
}
