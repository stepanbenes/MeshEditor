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
using MeshEditor.SolutionManager.IO;

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
				return Parser.Default.ParseArguments<ImportOptions, FilterOptions, CompressOptions, DiffOptions, ListOptions>(args)
					.MapResult(
					(ImportOptions options) => program.runImportCommand(options),
					(FilterOptions options) => program.runFilterCommand(options),
					(CompressOptions options) => program.runCompressCommand(options),
					(DiffOptions options) => program.runDiffCommand(options),
					(ListOptions options) => program.runListCommand(options),
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
			hub.Filter(pickSolution(hub), options.ParentLayer, options.FilterType, options.FilterParameters, options.LayerName);
			return 0;
		}

		private int runCompressCommand(CompressOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(logger);
			hub.Compress(pickSolution(hub), options.Layer, options.Method, options.FieldName, options.ComponentName);
			return 0;
		}

		private int runDiffCommand(DiffOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(logger);
			hub.Diff(pickSolution(hub), options.Layer);
			return 0;
		}

		private int runListCommand(ListOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(logger);
			foreach (var solutionInfo in hub.EnumerateSolutions()) // list all solutions
			{
				logger.LogMessage($"# Id: {solutionInfo.Id}, ProjectName: {solutionInfo.ProjectName}", LogMessagePriority.High);
				foreach (var layerInfo in hub.EnumerateLayersOfSolution(solutionInfo))
				{
					printLayerInfo(layerInfo, depth: 1);
				}
			}
			return 0;
		}

		private void printLayerInfo(ILayerInfo layerInfo, int depth)
		{
			logger.LogMessage($"{new string(' ', depth * 2)}+ {layerInfo.Name}, filter: {layerInfo.FilterType}, {layerInfo.Id}", LogMessagePriority.High);
			foreach (var child in layerInfo.Children)
			{
				printLayerInfo(child, depth + 1);
			}
		}

		#endregion

		#region Private methods

		private static ISolutionInfo pickSolution(SolutionHub hub)
		{
			return hub.EnumerateSolutions().First();
		}

		#endregion
	}
}
