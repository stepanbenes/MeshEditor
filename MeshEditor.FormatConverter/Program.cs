using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MeshEditor.IO;
using System.Reflection;
using System.IO;
using MeshEditor.Construction;
using MeshEditor.Data;
using MeshEditor.CoreInterface;
using System.Threading;
using System.Globalization;
using OpenTK;
using System.Text.RegularExpressions;
using MeshEditor.DataVisualizer.IO;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Import;
using MeshEditor.FormatConverter.Storage;
using MeshEditor.FormatConverter.Import;
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

			var masterLayerGenerator = new MasterLayerGenerator(storageService);

			IGeometryImportService geometryImportService = GeometryImportServiceFactory.Create(storageService, Path.GetFileName(args.First()));
			IDataImportService dataImportService = (args.Length > 1) ? DataImportServiceFactory.Create(storageService, args.Skip(1).Select(arg => Path.GetFileName(arg))) : null;
			masterLayerGenerator.Generate(Path.GetFileNameWithoutExtension(args[0]), geometryImportService, dataImportService);

			Console.Write("Done.");
		}
	}
}
