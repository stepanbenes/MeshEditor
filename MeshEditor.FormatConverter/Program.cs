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

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static int Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			return Parser.Default.ParseArguments<ImportOptions, FilterOptions>(args)
				.MapResult(
				(ImportOptions options) => runImportCommand(options),
				(FilterOptions options) => runFilterCommand(options),
				(CompressOptions options) => runCompressCommand(options),
				errors => 1);

			//if (args.Length < 1)
			//{
			//	Console.WriteLine("Usage: {0} mesh-file [result-files]", Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase));
			//	Console.ReadKey();
			//	return;
			//}

			//IStorageService storageService = new LocalFileSystemStorageService();
			//IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(storageService, new Uri(args[0]));
			//IDataImportService dataImportService = (args.Length > 1) ? DataFormatParserFactory.Create(storageService, args.Skip(1).Select(arg => new Uri(arg))) : null;

			//new LayerGenerator(storageService).Generate(Path.GetFileNameWithoutExtension(args[0]), new Uri(Path.GetDirectoryName(args[0])), geometryImportService, dataImportService);

			//Console.Write("Done.");
		}

		private static int runImportCommand(ImportOptions options)
		{
			const string masterLayerName = "master";
			string projectNameAsValidFileName = options.ProjectName.MakeAlphanumericFilename();
			string currentDirectory = Directory.GetCurrentDirectory();

			IStorageService localStorage = new LocalFileSystemStorageService();
			IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(localStorage, convertToUri(options.MeshFile, currentDirectory));
			IDataImportService dataImportService = (options.ResultFiles != null) ? DataFormatParserFactory.Create(localStorage, options.ResultFiles.Select(arg => convertToUri(arg, currentDirectory))) : null;
			var layerGenerator = new LayerGenerator();
			var masterLayer = layerGenerator.GenerateMasterLayer(new Uri(currentDirectory + "/"), masterLayerName, geometryImportService, dataImportService);

			var solution = SolutionBuilder.CreateSolutionFromMasterLayer(masterLayer, options.ProjectName);

			using (Stream stream = localStorage.Save(new Uri(Path.Combine(currentDirectory, $"{projectNameAsValidFileName}.solution.json"))))
			{
				ISerializationService serializer = new JsonSerializationService();
				serializer.Serialize(solution, stream);
			}

			return 0;
		}

		private static int runFilterCommand(FilterOptions options)
		{
			FilterBase filter;
			switch (options.FilterType)
			{
				case FilterType.AttributeSelection:
					{
						var attributeFilter = new AttributeSelectionFilter();
						attributeFilter.AttributeName = options.FilterParameters.First();
						attributeFilter.AttributeSelection = options.FilterParameters.Skip(1).Select(p => int.Parse(p)).ToArray();
						filter = attributeFilter;
					}
					break;
				case FilterType.Surface:
				case FilterType.Slice:
				case FilterType.Clip:
				case FilterType.IsoSurface:
				case FilterType.StreamLines:
					throw new NotImplementedException();
				default:
					throw new NotSupportedException();
			}

			string currentDirectory = Directory.GetCurrentDirectory();
			string solutionFilePath = Directory.EnumerateFiles(currentDirectory, "*.solution.json", SearchOption.TopDirectoryOnly).Single();

			IStorageService localStorage = new LocalFileSystemStorageService();

			Solution solution;
			using (Stream stream = localStorage.Load(new Uri(solutionFilePath)))
			{
				ISerializationService serializer = new JsonSerializationService();
				solution = serializer.Deserialize<Solution>(stream);
			}

			Solution.LayerRecord parentLayer;
			Guid guid;
			if (Guid.TryParse(options.ParentLayer, out guid))
			{
				parentLayer = findLayer(solution.Layers, layer => layer.Id == guid);
			}
			else
			{
				parentLayer = findLayer(solution.Layers, layer => string.Equals(layer.Name, options.ParentLayer, StringComparison.InvariantCultureIgnoreCase));
				if (parentLayer == null)
				{
					throw new Exception($"Layer '{options.ParentLayer}' not found.");
				}
			}

			var layerGenerator = new LayerGenerator();
			var filterLayer = layerGenerator.GenerateFilterLayer(new Uri(currentDirectory + "/"), parentLayer.Id, filter, options.LayerName);

			// convert filter layer to layer record and append it to parent layer's children
			parentLayer.Children = parentLayer.Children.EmptyIfNull().Append(SolutionBuilder.CreateLayerRecordFromFilterLayer(filterLayer)).ToArray();

			using (Stream stream = localStorage.Save(new Uri(solutionFilePath)))
			{
				ISerializationService serializer = new JsonSerializationService();
				serializer.Serialize(solution, stream);
			}

			return 0;
		}

		private static int runCompressCommand(CompressOptions options)
		{
			throw new NotImplementedException();

			return 0;
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

		private static Uri convertToUri(string path, string basePath)
		{
			if (Path.IsPathRooted(path))
			{
				return new Uri(path);
			}
			return new Uri(Path.Combine(basePath, path));
		}
	}
}
