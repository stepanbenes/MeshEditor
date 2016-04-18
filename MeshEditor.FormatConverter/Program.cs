using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Globalization;
using CommandLine;
using System.Collections.Generic;
using System.Diagnostics;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.Logging;

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

		ILogger logger;
		Stopwatch stopwatch;

		public Program()
		{
			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		public void GoodBye()
		{
			stopwatch.Stop();
			logger?.LogMessage($"Done in {stopwatch.Elapsed.ToString("mm':'ss'.'ff")}.", LogMessagePriority.High);
		}

		#endregion

		#region Commands

		private int runImportCommand(ImportOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			new SolutionHub(logger)
				.Import(options.ProjectName, options.MeshFile, options.ResultFiles);
			return 0;
		}

		private int runFilterCommand(FilterOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(logger);
			hub.Filter(hub.EnumerateSolutions().Single(), options.ParentLayer, options.FilterType, options.FilterParameters, options.LayerName);
			return 0;
		}

		private int runCompressCommand(CompressOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(logger);
			hub.Compress(hub.EnumerateSolutions().Single(), options.Layer, options.Method, options.FieldName, options.ComponentName);
			return 0;
		}

		private int runDiffCommand(DiffOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(logger);
			hub.Diff(hub.EnumerateSolutions().Single(), options.Layer);
			return 0;
		}

		#endregion

		#region Private methods

		#endregion
	}
}
