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

		string solutionFile;
		IStorageService importStorage, layerSourceStorage, layerDestinationStorage;
		ILogger logger;

		public SolutionHub(string solutionFile, ILogger logger = null)
		{
			this.solutionFile = solutionFile;
			this.logger = logger;
			ConfigLoader.ReadConfiguration(out importStorage, out layerSourceStorage, out layerDestinationStorage);
		}

		#endregion

		#region Commands (SolutionManager's public interface)

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

			string solutionDirectory;
			string solutionRecordName;

			if (string.IsNullOrEmpty(solutionFile))
			{
				solutionDirectory = Directory.GetCurrentDirectory();
				string projectNameAsValidFileName = projectName.MakeAlphanumericFilename();
				solutionRecordName = $"{projectNameAsValidFileName}.solution.json";
			}
			else
			{
				solutionDirectory = Path.GetDirectoryName(solutionFile);
				solutionRecordName = Path.GetFileName(solutionFile);
			}

			IStorageService solutionStorage = new LocalFileSystemStorageService(solutionDirectory);
			using (Stream stream = solutionStorage.Save(solutionRecordName))
			{
				ISerializationService solutionSerializer = new JsonSerializationService();
				solutionSerializer.Serialize(solution, stream);
			}
		}

		public void Filter(string parentLayerIdOrName, string filterTypeName, IEnumerable<string> filterParameters, string layerName)
		{
			FilterType filterType;
			if (!Enum.TryParse(filterTypeName, ignoreCase: true, result: out filterType))
				throw new ArgumentException($"Unknown filter type ({filterTypeName})", nameof(filterTypeName));

			Filter filter = FilterFactory.Create(filterType, filterParameters);

			processLayer(solutionFile, parentLayerIdOrName,
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

		public void Compress(string layerIdOrName, string compressionMethodName, string fieldName, string componentName)
		{
			CompressionMethod method;
			if (!Enum.TryParse(compressionMethodName, ignoreCase: true, result: out method))
				throw new ArgumentException($"Unknown compression method ({compressionMethodName})", nameof(compressionMethodName));

			processLayer(solutionFile, layerIdOrName,
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

		public void Diff(string layerIdOrName)
		{
			processLayer(solutionFile, layerIdOrName,
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

		private static SolutionFile createSolutionFromMasterLayer(SummaryLayerFile masterLayer, string projectName)
		{
			SolutionFile solution = new SolutionFile
			{
				ProjectName = projectName,
				Layers = new[]
				{
					new SolutionFile.LayerRecord
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

		private static SolutionFile.LayerRecord createLayerRecordFromFilterLayer(SummaryLayerFile filterLayer)
		{
			var newLayerRecord = new SolutionFile.LayerRecord
			{
				Id = filterLayer.Id,
				Name = filterLayer.Name,
				Filter = filterLayer.Filter,
				Children = null
			};
			return newLayerRecord;
		}

		private void processLayer(string solutionFile, string layerIdentifier, Action<SolutionFile.LayerRecord> processLayerOperation, bool updateSolutionFile)
		{
			IStorageService solutionStorage = new LocalFileSystemStorageService(Path.GetDirectoryName(solutionFile));
			ISerializationService solutionSerializer = new JsonSerializationService();
			SolutionFile solution;
			string recordName = Path.GetFileName(solutionFile);
			using (Stream stream = solutionStorage.Load(recordName))
			{
				solution = solutionSerializer.Deserialize<SolutionFile>(stream);
			}

			// find layer according to either provided layer guid or layer name
			SolutionFile.LayerRecord layer;
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
				using (Stream stream = solutionStorage.Save(recordName))
				{
					solutionSerializer.Serialize(solution, stream);
				}
			}
		}

		private static SolutionFile.LayerRecord findLayer(IEnumerable<SolutionFile.LayerRecord> layers, Func<SolutionFile.LayerRecord, bool> predicate)
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
