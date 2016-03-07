using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Globalization;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Import;
using MeshEditor.FormatConverter.Import;
using MeshEditor.LayerManager.Storage;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			if (args.Length < 1)
			{
				Console.WriteLine("Usage: {0} mesh-file [result-files]", Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase));
				Console.ReadKey();
				return;
			}

			IStorageService storageService = new LocalFileSystemStorageService(Path.GetDirectoryName(args[0]));
			ILayerSerializer layerSerializer = new JsonLayerSerializer();
			var masterLayerGenerator = new MasterLayerGenerator(storageService, layerSerializer);

			IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(storageService, Path.GetFileName(args.First()));
			IDataImportService dataImportService = (args.Length > 1) ? DataFormatParserFactory.Create(storageService, args.Skip(1).Select(arg => Path.GetFileName(arg))) : null;
			masterLayerGenerator.Generate(Path.GetFileNameWithoutExtension(args[0]), geometryImportService, dataImportService);

			Console.Write("Done.");
		}
	}
}
