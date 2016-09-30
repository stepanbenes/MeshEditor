using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.LayerManager;
using MeshEditor.Common;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;
using MeshEditor.SolutionManager.AzureStorage;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.SolutionManager
{
	public class SolutionHub
	{
		#region Static fields, static constructor

		readonly static LocalStorageConfiguration localStorageConfiguration;
		readonly static AzureBlobStorageConfiguration azureBlobStorageConfiguration;
		readonly static RestApiConfiguration restApiConfiguration;

		readonly static string DefaultMasterLayerName = "master";

		static SolutionHub()
		{
			localStorageConfiguration = ConfigurationManager.ReadConfigurationObject<LocalStorageConfiguration>("LocalStorage") ?? new LocalStorageConfiguration();
			azureBlobStorageConfiguration = ConfigurationManager.ReadConfigurationObject<AzureBlobStorageConfiguration>("AzureBlobStorage") ?? new AzureBlobStorageConfiguration();
			restApiConfiguration = ConfigurationManager.ReadConfigurationObject<RestApiConfiguration>("RestApi") ?? new RestApiConfiguration();
		}

		#endregion

		#region Public static methods

		public static string GetLocalStorageDefaultDirectory()
		{
			var folder = localStorageConfiguration.Directory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			return folder;
		}

		public static void SetLocalStorageDefaultDirectory(string directoryPath)
		{
			localStorageConfiguration.Directory = directoryPath;
			ConfigurationManager.WriteConfigurationObject("LocalStorage", localStorageConfiguration);
		}

		public static SolutionHub CreateEmptyLocal(string solutionDirectory, ILogger logger = null)
		{
			var solutionController = new LocalSolutionController(solutionDirectory);

			IStorageService localStorage = new LocalFileSystemStorageService(solutionDirectory);
			return new SolutionHub(
				solutionLocator: null,
				solutionController: solutionController,
				importStorage: localStorage,
				layerSourceStorage: localStorage,
				layerDestinationStorage: localStorage,
				logger: logger
			);
		}

		public static SolutionHub CreateLocal(string solutionFileName, ILogger logger = null)
		{
			Debug.Assert(solutionFileName != null);
			string solutionDirectory = Path.GetDirectoryName(solutionFileName);
			var solutionController = new LocalSolutionController(solutionDirectory);

			IStorageService localStorage = new LocalFileSystemStorageService(solutionDirectory);
			return new SolutionHub(
				solutionLocator: solutionFileName,
				solutionController: solutionController,
				importStorage: localStorage,
				layerSourceStorage: localStorage,
				layerDestinationStorage: localStorage,
				logger: logger
			);
		}

		public static SolutionHub CreateRemote(int solutionId, ILogger logger = null)
		{
			return new SolutionHub(
				solutionLocator: solutionId,
				solutionController: new RestApiSolutionController(restApiConfiguration.Uri, logger),
				importStorage: new AzureBlobStorageService(azureBlobStorageConfiguration.ConnectionString, azureBlobStorageConfiguration.ResultsBlobContainerName),
				layerSourceStorage: new AzureBlobStorageService(azureBlobStorageConfiguration.ConnectionString, azureBlobStorageConfiguration.LayersBlobContainerName),
				layerDestinationStorage: new AzureBlobStorageService(azureBlobStorageConfiguration.ConnectionString, azureBlobStorageConfiguration.LayersBlobContainerName),
				logger: logger
			);
		}

		public static IEnumerable<ISolutionInfo> EnumerateAllLocalSolutions(string solutionDirectory, bool includeOneSubDirectory, ILogger logger = null)
		{
			var solutionController = new LocalSolutionController(solutionDirectory);
			return solutionController.GetAll(includeOneSubDirectory);
		}

		public static async Task<IEnumerable<ISolutionInfo>> EnumerateAllLocalSolutionsAsync(string solutionDirectory, bool includeOneSubDirectory, CancellationToken cancellationToken, ILogger logger = null)
		{
			var solutionController = new LocalSolutionController(solutionDirectory);
			return await solutionController.GetAllAsync(includeOneSubDirectory, cancellationToken);
		}

		public static IEnumerable<ISolutionInfo> EnumerateAllRemoteSolutions(ILogger logger = null)
		{
			var solutionController = new RestApiSolutionController(restApiConfiguration.Uri, logger);
			return solutionController.GetAll();
		}

		public static async Task<IEnumerable<ISolutionInfo>> EnumerateAllRemoteSolutionsAsync(CancellationToken cancellationToken, ILogger logger = null)
		{
			var solutionController = new RestApiSolutionController(restApiConfiguration.Uri, logger);
			return await solutionController.GetAllAsync(cancellationToken);
		}

		#endregion

		#region Fields, Constructors

		object solutionLocator;
		ISolutionController solutionController;
		IStorageService importStorage, layerSourceStorage, layerDestinationStorage;
		ILogger logger;

		private SolutionHub(object solutionLocator, ISolutionController solutionController, IStorageService importStorage, IStorageService layerSourceStorage, IStorageService layerDestinationStorage, ILogger logger = null)
		{
			this.solutionLocator = solutionLocator;
			this.solutionController = solutionController;
			this.importStorage = importStorage;
			this.layerSourceStorage = layerSourceStorage;
			this.layerDestinationStorage = layerDestinationStorage;
			this.logger = logger;
		}

		#endregion

		#region Commands (SolutionManager's public interface)

		public ISolutionDescription GetSolutionDescription()
		{
			return solutionController.Get(solutionLocator);
		}

		public async Task<ISolutionDescription> GetSolutionDescriptionAsync(CancellationToken cancellationToken)
		{
			return await solutionController.GetAsync(solutionLocator, cancellationToken);
		}

		public Task<GeometryDescription> LoadGeometryAsync(Guid layerId, int meshIndex, CancellationToken cancellationToken)
		{
			var layerGenerator = new LayerGenerator(layerSourceStorage, destinationStorage: null, logger: logger);
			return layerGenerator.LoadGeometryAsync(layerId, meshIndex, cancellationToken);
		}

		public Task<IEnumerable<ComponentDataDescription>> LoadDataAsync(Guid layerId, int dataIndex, CancellationToken cancellationToken)
		{
			var layerGenerator = new LayerGenerator(layerSourceStorage, destinationStorage: null, logger: logger);
			return layerGenerator.LoadDataAsync(layerId, dataIndex, cancellationToken);
		}

		public Task<AttributeDescription> LoadAttributeAsync(Guid layerId, int attributeIndex, CancellationToken cancellationToken)
		{
			var layerGenerator = new LayerGenerator(layerSourceStorage, destinationStorage: null, logger: logger);
			return layerGenerator.LoadAttributeAsync(layerId, attributeIndex, cancellationToken);
		}

		public Task<SummaryFile> LoadLayerSummaryAsync(Guid layerId, CancellationToken cancellationToken)
		{
			var layerGenerator = new LayerGenerator(layerSourceStorage, destinationStorage: null, logger: logger);
			return layerGenerator.LoadLayerSummaryAsync(layerId, cancellationToken);
		}

		public string Create(IEnumerable<AnalysisResult> analysisResults, string projectName = null)
		{
			Solution solution = solutionController.CreateNew(solutionLocator, analysisResults, projectName);
			solutionLocator = solution.Location;
			logger?.LogMessage($"Created at '{solution.Location}'");
			return solution.Location;
		}

		public void Import(IEnumerable<double> keyTimeSteps, IEnumerable<string> compressionParameters, string gaussPointsExtrapolationStrategyName = null, string fieldName = null, string masterLayerName = null)
		{
			//Debug.Assert(!keyTimeSteps.Any());

			Solution solution = solutionController.Get(solutionLocator);

			var analysisResultImportServices = solution.Results?.Select(result => AnalysisResultImportServiceFactory.Create(importStorage, result, gaussPointsExtrapolationStrategyName)) ?? Enumerable.Empty<IAnalysisResultImportService>();
			
			var layerGenerator = new LayerGenerator(
										sourceStorage: layerSourceStorage,
										destinationStorage: layerDestinationStorage,
										compressionService: CompressionServiceFactory.Create(compressionParameters, logger),
										logger: logger);

			var masterLayerSummaryFile = layerGenerator.GenerateMasterLayer(masterLayerName ?? DefaultMasterLayerName, analysisResultImportServices, keyTimeSteps, fieldName);
			
			var masterLayer = createLayerRecordFromLayerSummaryFile(masterLayerSummaryFile);
			logNewLayer(masterLayer);
			Solution updatedSolution = solutionController.AddLayer(solution, parentLayer: null, newLayer: masterLayer);
		}

		//public void ImportWithTemporaryStorageOptimization(IEnumerable<int> analysisResultGroupLengths, IEnumerable<string> analysisResultRecordNames, IEnumerable<double> keyTimeSteps, IEnumerable<string> compressionParameters, string gaussPointsExtrapolationStrategyName = null, string fieldName = null, string masterLayerName = null)
		//{
		//	Debug.Assert(keyTimeSteps.Any());

		//	Solution solution = solutionController.Get(solutionLocator);

		//	var analysisResults = composeAnalysisResults(analysisResultGroupLengths, analysisResultRecordNames);

		//	var analysisResultImportServices = analysisResults.Select(result => AnalysisResultImportServiceFactory.Create(meshImportStorage, dataImportStorage, result, gaussPointsExtrapolationStrategyName));

		//	string tempPath = Path.GetTempPath();
		//	logger.LogOperationProgress("Creating temporary master layer in folder: " + tempPath);

		//	IStorageService tempStorageService = new LocalFileSystemStorageService(tempPath);

		//	var layerGeneratorForMaster = new LayerGenerator(
		//								sourceStorage: layerSourceStorage,
		//								destinationStorage: tempStorageService, // save temporary master in local temp folder
		//								compressionService: CompressionServiceFactory.Create(CompressionMethod.Transparent), // do not use compression for temporary master
		//								logger: logger);

		//	// pass empty key time steps to temporary master
		//	var masterLayerSummaryFile = layerGeneratorForMaster.GenerateMasterLayer("temporary_master", analysisResultImportServices, keyTimeSteps: Enumerable.Empty<double>(), fieldName: fieldName);

		//	var masterLayer = createLayerRecordFromLayerSummaryFile(masterLayerSummaryFile);
		//	logNewLayer(masterLayer);

		//	// -----------------------------------
		//	logger.LogOperationProgress("Starting compression");

		//	// continue with generating actual layer by compressing temporary master
		//	var layerGeneratorForCompressedMaster = new LayerGenerator(
		//								sourceStorage: tempStorageService, // use temporary folder as source storage
		//								destinationStorage: layerDestinationStorage,
		//								compressionService: CompressionServiceFactory.Create(compressionParameters),
		//								logger: logger);

		//	// create new layer by compressing temporary master
		//	var compressedMasterLayerSummaryFile = layerGeneratorForCompressedMaster.CompressLayer(masterLayer.Id, keyTimeSteps, layerName: masterLayerName ?? DefaultMasterLayerName, fieldName: fieldName);
		//	compressedMasterLayerSummaryFile.ParentId = null; // this is new master, so set filter and parentId to null
		//	compressedMasterLayerSummaryFile.Filter = null;

		//	logger.LogOperationProgress("Deleting temporary master layer");
		//	LocalSolutionController.DeleteAllLayerFilesOfLayerTree(masterLayer, tempStorageService);
		//	logger.LogOperationProgress("Temporary master layer deleted");

		//	var compressedMasterLayer = createLayerRecordFromLayerSummaryFile(compressedMasterLayerSummaryFile);
		//	logNewLayer(compressedMasterLayer);

		//	Solution updatedSolution = solutionController.AddLayer(solution, parentLayer: null, newLayer: compressedMasterLayer);
		//}

		public void Filter(string parentLayerIdOrName, string filterTypeName, IEnumerable<string> filterParameters, IEnumerable<double> keyTimeSteps, IEnumerable<string> compressionParameters, string fieldName = null, string newLayerName = null)
		{
			FilterType filterType;
			if (!Enum.TryParse(filterTypeName, ignoreCase: true, result: out filterType))
				throw new ArgumentException($"Unknown filter type ({filterTypeName})", nameof(filterTypeName));

			Filter filter = FilterFactory.Create(filterType, filterParameters);
			Solution solution = solutionController.Get(solutionLocator);

			var parentLayer = findLayer(solution, parentLayerIdOrName);

			var layerGenerator = new LayerGenerator(
										sourceStorage: layerSourceStorage,
										destinationStorage: layerDestinationStorage,
										compressionService: CompressionServiceFactory.Create(compressionParameters, logger),
										logger: logger);

			var filterLayerSummaryFile = layerGenerator.GenerateFilterLayer(parentLayer.Id, filter, newLayerName, keyTimeSteps, fieldName);
			
			// convert filter layer to layer record and append it to parent layer's children
			var filterLayer = createLayerRecordFromLayerSummaryFile(filterLayerSummaryFile);
			logNewLayer(filterLayer);
			Solution updatedSolution = solutionController.AddLayer(solution, parentLayer, filterLayer);
		}

		public void Compress(string parentLayerIdOrName, IEnumerable<double> keyTimeSteps, IEnumerable<string> compressionParameters, string fieldName = null, string newLayerName = null)
		{
			Solution solution = solutionController.Get(solutionLocator);

			var parentLayer = findLayer(solution, parentLayerIdOrName);

			var layerGenerator = new LayerGenerator(
										sourceStorage: layerSourceStorage,
										destinationStorage: layerDestinationStorage,
										compressionService: CompressionServiceFactory.Create(compressionParameters, logger),
										logger: logger);

			var compressedLayerSummaryFile = layerGenerator.CompressLayer(parentLayer.Id, keyTimeSteps, newLayerName ?? $"compressed ({string.Join(" ", compressionParameters)})", fieldName);
			
			// convert filter layer to layer record and append it to parent layer's children
			var compressedLayer = createLayerRecordFromLayerSummaryFile(compressedLayerSummaryFile);
			logNewLayer(compressedLayer);
			Solution updatedSolution = solutionController.AddLayer(solution, parentLayer, compressedLayer);
		}

		public void Delete(string layerIdOrName, bool deleteAll = false)
		{
			Debug.Assert(!string.IsNullOrEmpty(layerIdOrName) ^ deleteAll);
			
			if (deleteAll)
			{
				solutionController.Delete(solutionLocator); // delete solution itself
			}
			else
			{
				Solution solution = solutionController.Get(solutionLocator);
				solution = solutionController.DeleteLayer(solution, findLayer(solution, layerIdOrName));
			}
		}

		public async Task DeleteAsync(CancellationToken cancellationToken, string layerIdOrName, bool deleteAll = false)
		{
			Debug.Assert(!string.IsNullOrEmpty(layerIdOrName) ^ deleteAll);

			if (deleteAll)
			{
				await solutionController.DeleteAsync(solutionLocator, cancellationToken); // delete solution itself
			}
			else
			{
				Solution solution = await solutionController.GetAsync(solutionLocator, cancellationToken);
				solution = await solutionController.DeleteLayerAsync(solution, findLayer(solution, layerIdOrName), cancellationToken);
			}
		}

		public void Diff(string layerIdOrName)
		{
			Solution solution = solutionController.Get(solutionLocator);
			var layer = findLayer(solution, layerIdOrName);

			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, logger: logger);
			logger?.LogMessage(ComponentDiff.GetTableHeader());
			var diff = layerGenerator.CreateDiff(layer.Id);
			logger?.LogMessage(diff.ToString());
		}

		#endregion

		#region Private helper methods

		private static Solution.Layer createLayerRecordFromLayerSummaryFile(SummaryFile layerSummary)
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

		private void logNewLayer(ILayerInfo layerInfo)
		{
			logger?.LogMessage($"New layer created (name: {layerInfo.Name}, id: {layerInfo.Id})");
		}

		#endregion
	}
}
