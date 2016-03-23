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

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static int Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			return Parser.Default.ParseArguments<ImportOptions, AddOptions>(args)
				.MapResult(
				(ImportOptions options) => runImportCommand(options),
				(AddOptions options) => runAddCommand(options),
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
			string projectDirectoryName = projectNameAsValidFileName;
			string currentDirectory = Directory.GetCurrentDirectory();

			if (!Directory.Exists(projectDirectoryName))
			{
				Directory.CreateDirectory(projectDirectoryName);
			}

			IStorageService storageService = new LocalFileSystemStorageService();
			IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(storageService, convertToUri(options.MeshFile, currentDirectory));
			IDataImportService dataImportService = (options.ResultFiles != null) ? DataFormatParserFactory.Create(storageService, options.ResultFiles.Select(arg => convertToUri(arg, currentDirectory))) : null;
			var layerGenerator = new LayerGenerator(storageService);
			var masterLayerGuid = layerGenerator.GenerateMasterLayer(convertToUri(projectDirectoryName, currentDirectory), masterLayerName, geometryImportService, dataImportService);

			Project project = new Project
			{
				Name = options.ProjectName,
				Layers = new[] { new Project.LayerRecord { Id = masterLayerGuid, Name = masterLayerName } }
			};

			string json = JsonConvert.SerializeObject(project, Formatting.Indented);

			if (options.Verbose)
			{
				Console.WriteLine(json);
			}

			File.WriteAllText(Path.Combine(currentDirectory, projectDirectoryName, $"{projectNameAsValidFileName}.project.json"), json, System.Text.Encoding.UTF8);

			return 0;
		}

		private static int runAddCommand(AddOptions options)
		{
			throw new NotImplementedException();
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
