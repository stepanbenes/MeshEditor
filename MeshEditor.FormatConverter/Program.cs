using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Globalization;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Import;
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

			IStorageService storageService = new LocalFileSystemStorageService();
			IGeometryImportService geometryImportService = GeometryFormatParserFactory.Create(storageService, new Uri(args[0]));
			IDataImportService dataImportService = (args.Length > 1) ? DataFormatParserFactory.Create(storageService, args.Skip(1).Select(arg => new Uri(arg))) : null;

			new LayerGenerator(storageService).Generate(Path.GetFileNameWithoutExtension(args[0]), new Uri(Path.GetDirectoryName(args[0])), geometryImportService, dataImportService);

			Console.Write("Done.");
		}
	}
}
