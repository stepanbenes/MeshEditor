using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Globalization;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;
using MeshEditor.LayerManager.Common;
using CommandLine;
using Newtonsoft.Json;
using MeshEditor.LayerManager.Infrastructure;
using MeshEditor.LayerManager.Filters;
using System.Collections.Generic;
using System.Diagnostics;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static int Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			return Parser.Default.ParseArguments<ImportOptions, FilterOptions, CompressOptions>(args)
				.MapResult(
				(ImportOptions options) => runImportCommand(options),
				(FilterOptions options) => runFilterCommand(options),
				(CompressOptions options) => runCompressCommand(options),
				(DiffOptions options) => runDiffCommand(options),
				errors => 1);
		}

		#region Commands

		private static int runImportCommand(ImportOptions options)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			IProgress<OperationState> progress = options.Verbose ? createProgressReporter() : null;

			const string masterLayerName = "master";
			string projectDirectory = getProjectLocation(options);

			IStorageService localStorage = new LocalFileSystemStorageService();
			IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(localStorage, convertToUri(options.MeshFile, projectDirectory));
			IDataImportService dataImportService = options.ResultFiles.Any() ? DataFormatParserFactory.Create(localStorage, options.ResultFiles.Select(arg => convertToUri(arg, projectDirectory))) : null;
			var layerGenerator = new LayerGenerator(progressReporter: progress);
			var masterLayer = layerGenerator.GenerateMasterLayer(new Uri(projectDirectory + "/"), masterLayerName, geometryImportService, dataImportService);

			string projectName = options.ProjectName ?? Path.GetFileNameWithoutExtension(options.MeshFile);

			var solution = SolutionBuilder.CreateSolutionFromMasterLayer(masterLayer, projectName);

			string projectNameAsValidFileName = projectName.MakeAlphanumericFilename();
			using (Stream stream = localStorage.Save(new Uri(Path.Combine(projectDirectory, $"{projectNameAsValidFileName}.solution.json"))))
			{
				ISerializationService serializer = new JsonSerializationService();
				serializer.Serialize(solution, stream);
			}

			stopwatch.Stop();
			clearCurrentConsoleLine();
			Console.WriteLine($"Done in {stopwatch.Elapsed.ToString("mm':'ss'.'ff")}.");

			return 0;
		}

		private static int runFilterCommand(FilterOptions options)
		{
			FilterBase filter = FilterFactory.Create(options.FilterType, options.FilterParameters);

			string projectDirectory = getProjectLocation(options);

			processLayer(projectDirectory, options.ProjectName, options.ParentLayer,
				layer =>
				{
					var layerGenerator = new LayerGenerator();
					var filterLayer = layerGenerator.GenerateFilterLayer(new Uri(projectDirectory), layer.Id, filter, options.LayerName);

					// convert filter layer to layer record and append it to parent layer's children
					var childLayer = SolutionBuilder.CreateLayerRecordFromFilterLayer(filterLayer);
					layer.Children = layer.Children.EmptyIfNull().Append(childLayer).ToArray();
				},
				updateSolutionFile: true
			);

			return 0;
		}

		private static int runCompressCommand(CompressOptions options)
		{
			string projectDirectory = getProjectLocation(options);

			processLayer(projectDirectory, options.ProjectName, options.Layer,
				layer =>
				{
					var layerGenerator = new LayerGenerator(compressionService: CompressionServiceFactory.Create(options.Method));
					var compressedLayer = layerGenerator.CompressLayer(new Uri(projectDirectory), layer.Id, $"time compression ({options.Method})", options.FieldName, options.ComponentName);

					// convert filter layer to layer record and append it to parent layer's children
					var childLayer = SolutionBuilder.CreateLayerRecordFromFilterLayer(compressedLayer);
					layer.Children = layer.Children.EmptyIfNull().Append(childLayer).ToArray();
				},
				updateSolutionFile: true
			);

			return 0;
		}

		private static int runDiffCommand(DiffOptions options)
		{
			string projectDirectory = getProjectLocation(options);

			processLayer(projectDirectory, options.ProjectName, options.Layer,
				layer =>
				{
					var layerGenerator = new LayerGenerator();
					var diff = layerGenerator.CreateDiff(new Uri(projectDirectory), layer.Id);
					Console.WriteLine(diff);
				},
				updateSolutionFile: false
			);

			return 0;
		}

		#endregion

		#region Helper methods

		private static string getProjectLocation(Options options)
		{
			return options.Directory ?? Directory.GetCurrentDirectory() + "/";
		}

		private static void processLayer(string solutionDirectory, string optionalProjectName, string layerIdentifier, Action<Solution.LayerRecord> processLayerOperation, bool updateSolutionFile)
		{
			var solutionFiles = Directory.EnumerateFiles(solutionDirectory, "*.solution.json", SearchOption.TopDirectoryOnly);
			IStorageService localStorage = new LocalFileSystemStorageService();
			ISerializationService serializer = new JsonSerializationService();
			Solution solution = null;
			string solutionFilePath = null;
			if (optionalProjectName != null) // if project name is set, enumerate all solution files and find project name match
			{
				foreach (var path in solutionFiles)
				{
					Solution testSolution;
					using (Stream stream = localStorage.Load(new Uri(path)))
					{
						testSolution = serializer.Deserialize<Solution>(stream);
					}
					if (optionalProjectName.Equals(testSolution.ProjectName))
					{
						if (solution != null)
						{
							throw new InvalidOperationException($"Directory contains more than one solution file with project name '{optionalProjectName}'");
						}
						solution = testSolution;
						solutionFilePath = path;
					}
				}

				if (solution == null)
				{
					throw new FileNotFoundException();
				}

				Debug.Assert(solutionFilePath != null);
			}
			else // if project name is NOT set, find single solution file in directory and load solution object
			{
				solutionFilePath = solutionFiles.Single();
				using (Stream stream = localStorage.Load(new Uri(solutionFilePath)))
				{
					solution = serializer.Deserialize<Solution>(stream);
				}
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
				using (Stream stream = localStorage.Save(new Uri(solutionFilePath)))
				{
					serializer.Serialize(solution, stream);
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

		private static IProgress<OperationState> createProgressReporter()
		{
			return new Progress<OperationState>
				(
					state =>
					{
						clearCurrentConsoleLine();
						Console.Write(state.State);
						if (state.PercentDone.HasValue)
						{
							Console.Write(state.PercentDone);
						}
						Console.Write(" ");
					}
				);
		}

		private static void clearCurrentConsoleLine()
		{
			int currentLine = Console.CursorTop;
			Console.SetCursorPosition(0, currentLine);
			Console.Write(new string(' ', Console.WindowWidth));
			Console.SetCursorPosition(0, currentLine);
		}

		private static Uri convertToUri(string path, string basePath)
		{
			if (Path.IsPathRooted(path))
			{
				return new Uri(path);
			}
			return new Uri(Path.Combine(basePath, path));
		}

		#endregion
	}
}
