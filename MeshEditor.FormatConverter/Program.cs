using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Globalization;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;
using MeshEditor.LayerManager.Common;
using CommandLine;
using MeshEditor.LayerManager.Infrastructure;
using MeshEditor.LayerManager.Filters;
using System.Collections.Generic;
using System.Diagnostics;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Data;
using System.Reflection;

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static int Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			var program = new Program();
			try
			{
				return Parser.Default.ParseArguments<ImportOptions, FilterOptions, CompressOptions, DiffOptions>(args)
					.MapResult(
					(ImportOptions options) => program.runImportCommand(options),
					(FilterOptions options) => program.runFilterCommand(options),
					(CompressOptions options) => program.runCompressCommand(options),
					(DiffOptions options) => program.runDiffCommand(options),
					errors => 1);
			}
			finally
			{
				program.GoodBye();
			}
		}

		#region Fields, constructor

		IStorageService importStorage, layerSourceStorage, layerDestinationStorage;
		Stopwatch stopwatch;

		public Program()
		{
			readConfiguration();

			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		public void GoodBye()
		{
			stopwatch.Stop();
			logMessage($"Done in {stopwatch.Elapsed.ToString("mm':'ss'.'ff")}.");
		}

		#endregion

		#region Commands

		private int runImportCommand(ImportOptions options)
		{
			const string masterLayerName = "master";
			IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(importStorage, options.MeshFile);
			IDataImportService dataImportService = options.ResultFiles.Any() ? DataFormatParserFactory.Create(importStorage, options.ResultFiles) : null;
			var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter(options));
			var masterLayer = layerGenerator.GenerateMasterLayer(masterLayerName, geometryImportService, dataImportService);
			logNewLayer(masterLayer);
			string projectName = options.ProjectName ?? Path.GetFileNameWithoutExtension(options.MeshFile);
			var solution = SolutionBuilder.CreateSolutionFromMasterLayer(masterLayer, projectName);
			

			string solutionDirectory;
			string solutionRecordName;

			if (string.IsNullOrEmpty(options.SolutionFile))
			{
				solutionDirectory = Directory.GetCurrentDirectory();
				string projectNameAsValidFileName = projectName.MakeAlphanumericFilename();
				solutionRecordName = $"{projectNameAsValidFileName}.solution.json";
			}
			else
			{
				solutionDirectory = Path.GetDirectoryName(options.SolutionFile);
				solutionRecordName = Path.GetFileName(options.SolutionFile);
			}

			IStorageService solutionStorage = new LocalFileSystemStorageService(solutionDirectory);
			using (Stream stream = solutionStorage.Save(solutionRecordName))
			{
				ISerializationService solutionSerializer = new JsonSerializationService();
				solutionSerializer.Serialize(solution, stream);
			}
			return 0;
		}

		private int runFilterCommand(FilterOptions options)
		{
			Filter filter = FilterFactory.Create(options.FilterType, options.FilterParameters);
			processLayer(getSolutionFile(options), options.ParentLayer,
				layer =>
				{
					var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter(options));
					var filterLayer = layerGenerator.GenerateFilterLayer(layer.Id, filter, options.LayerName);
					logNewLayer(filterLayer);
					// convert filter layer to layer record and append it to parent layer's children
					var childLayer = SolutionBuilder.CreateLayerRecordFromFilterLayer(filterLayer);
					layer.Children = layer.Children.EmptyIfNull().Append(childLayer).ToArray();
				},
				updateSolutionFile: true
			);
			return 0;
		}

		private int runCompressCommand(CompressOptions options)
		{
			processLayer(getSolutionFile(options), options.Layer,
				layer =>
				{
					var layerGenerator = new LayerGenerator(compressionService: CompressionServiceFactory.Create(options.Method), sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter(options));
					var compressedLayer = layerGenerator.CompressLayer(layer.Id, $"time compression ({options.Method})", options.FieldName, options.ComponentName);
					logNewLayer(compressedLayer);
					// convert filter layer to layer record and append it to parent layer's children
					var childLayer = SolutionBuilder.CreateLayerRecordFromFilterLayer(compressedLayer);
					layer.Children = layer.Children.EmptyIfNull().Append(childLayer).ToArray();
				},
				updateSolutionFile: true
			);
			return 0;
		}

		private int runDiffCommand(DiffOptions options)
		{
			processLayer(getSolutionFile(options), options.Layer,
				layer =>
				{
					var layerGenerator = new LayerGenerator(sourceStorage: layerSourceStorage, destinationStorage: layerDestinationStorage, progressReporter: createProgressReporter(options));
					var diff = layerGenerator.CreateDiff(layer.Id);
					logMessage(diff.ToString());
				},
				updateSolutionFile: false
			);
			return 0;
		}

		#endregion

		#region Helper methods

		private static string getSolutionFile(Options options)
		{
			if (string.IsNullOrEmpty(options.SolutionFile)) // if project name is set, enumerate all solution files and find project name match
			{
				var solutionFiles = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.solution.json", SearchOption.TopDirectoryOnly);
				return solutionFiles.Single();
			}
			return options.SolutionFile;
		}

		private void processLayer(string solutionFile, string layerIdentifier, Action<Solution.LayerRecord> processLayerOperation, bool updateSolutionFile)
		{
			IStorageService solutionStorage = new LocalFileSystemStorageService(Path.GetDirectoryName(solutionFile));
			ISerializationService solutionSerializer = new JsonSerializationService();
			Solution solution;
			string recordName = Path.GetFileName(solutionFile);
			using (Stream stream = solutionStorage.Load(recordName))
			{
				solution = solutionSerializer.Deserialize<Solution>(stream);
			}

			// find layer according to either provided layer guid or layer name
			Solution.LayerRecord layer;
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

		private static Solution.LayerRecord findLayer(IEnumerable<Solution.LayerRecord> layers, Func<Solution.LayerRecord, bool> predicate)
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

		private IProgress<OperationState> createProgressReporter(Options options)
		{
			return options.Verbose ? new Progress<OperationState>
				(
					state => logMessage(state.State)
				)
				:
				null;
		}

		private void logNewLayer(SummaryLayerFile layerSummary)
		{
			logMessage($"layer name: {layerSummary.Name}, layer id: {layerSummary.Id}");
		}

		//private static void clearCurrentConsoleLine()
		//{
		//	int currentLine = Console.CursorTop;
		//	Console.SetCursorPosition(0, currentLine);
		//	Console.Write(new string(' ', Console.WindowWidth));
		//	Console.SetCursorPosition(0, currentLine);
		//}

		private static Uri convertToUri(string path, string basePath)
		{
			if (Path.IsPathRooted(path))
			{
				return new Uri(path);
			}
			return new Uri(Path.Combine(basePath, path));
		}

		private void logMessage(string message)
		{
			Console.WriteLine(message);
		}

		private void readConfiguration()
		{
			string configFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
			if (File.Exists(configFilename))
			{
				Config config = null;
				using (var stream = new FileStream(configFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					ISerializationService serializer = new JsonSerializationService();
					config = serializer.Deserialize<Config>(stream);
				}

				importStorage = createStorageService(config.ImportStorage);
				layerSourceStorage = createStorageService(config.LayerSourceStorage);
				layerDestinationStorage = createStorageService(config.LayerDestinationStorage);
			}
			else
			{
				importStorage = layerSourceStorage = layerDestinationStorage = new LocalFileSystemStorageService(Directory.GetCurrentDirectory());
			}
		}

		private static IStorageService createStorageService(StorageInfo storageInfo)
		{
			switch (storageInfo.Type)
			{
				case StorageType.Local:
					{
						var localStorageInfo = (LocalStorageInfo)storageInfo;
						return new LocalFileSystemStorageService(localStorageInfo.Directory ?? Directory.GetCurrentDirectory());
					}
				case StorageType.AzureBlob:
					{
						var azureBlobStorageInfo = (AzureBlobStorageInfo)storageInfo;
						return new AzureBlobStorageService(azureBlobStorageInfo.ConnectionString, azureBlobStorageInfo.BlobContainerName);
					}
				default:
					throw new NotSupportedException();
			}
		}

		#endregion
	}
}
