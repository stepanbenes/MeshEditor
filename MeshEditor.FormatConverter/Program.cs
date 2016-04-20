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
			int code;
			try
			{
				code = Parser.Default.ParseArguments<ImportOptions, FilterOptions, CompressOptions, DiffOptions, ListOptions>(args)
					.MapResult(
					(ImportOptions options) => program.runImportCommand(options),
					(FilterOptions options) => program.runFilterCommand(options),
					(CompressOptions options) => program.runCompressCommand(options),
					(DiffOptions options) => program.runDiffCommand(options),
					(ListOptions options) => program.runListCommand(options),
					errors => 1);
			}
			catch (Exception ex)
			{
				program.logger?.LogError(ex);
				code = 1;
			}
			program.goodBye(success: code == 0);
			return code;
		}

		#region Fields, constructor

		ILogger logger;
		Stopwatch stopwatch;

		public Program()
		{
			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		#endregion

		#region Commands

		private int runImportCommand(ImportOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			new SolutionHub(options.ConfigFile, logger)
				.Import(options.ProjectName, options.MeshFile, options.ResultFiles);
			return 0;
		}

		private int runFilterCommand(FilterOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(options.ConfigFile, logger);
			hub.Filter(pickSolution(hub), options.ParentLayer, options.FilterType, options.FilterParameters, options.LayerName);
			return 0;
		}

		private int runCompressCommand(CompressOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(options.ConfigFile, logger);
			hub.Compress(pickSolution(hub), options.Layer, options.Method, options.FieldName, options.ComponentName);
			return 0;
		}

		private int runDiffCommand(DiffOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(options.ConfigFile, logger);
			hub.Diff(pickSolution(hub), options.Layer);
			return 0;
		}

		private int runListCommand(ListOptions options)
		{
			logger = new ConsoleLogger(options.Verbose);
			var hub = new SolutionHub(options.ConfigFile, logger);
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

		private ISolutionInfo pickSolution(SolutionHub hub)
		{
			var solutions = hub.EnumerateSolutions().ToArray();
			if (solutions.Length == 0)
				throw new FileNotFoundException("No solution found");
			if (solutions.Length == 1)
				return solutions[0];

			stopwatch.Stop(); // interrupt stopwatch
			try
			{
				// otherwise show menu:
				Console.WriteLine("Choose solution:");
				foreach (var solution in solutions)
				{
					Console.WriteLine($"# Solution id: {solution.Id}, Project name: '{solution.ProjectName}'");
				}

				// read input from keyboard
				while (true)
				{
					Console.Write("Id = ");
					string input = Console.ReadLine();
					int id;
					if (!int.TryParse(input, out id))
					{
						Console.WriteLine("Please insert valid integer value.");
						continue;
					}
					if (!solutions.Any(s => s.Id == id))
					{
						Console.WriteLine($"Solution with id '{id}' does not exist.");
						continue;
					}
					return solutions.Single(s => s.Id == id);
				}
			}
			finally
			{
				stopwatch.Start();
			}
		}

		private void goodBye(bool success)
		{
			stopwatch.Stop();
			logger?.LogMessage($"{(success ? "Success." : "Fail.")} Duration: {stopwatch.Elapsed.ToString("mm':'ss'.'ff")}", LogMessagePriority.High);
		}

		#endregion
	}
}
