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
using Microsoft.Azure.WebJobs;

namespace MeshEditor.FormatConverter
{
	enum StorageType
	{
		Local,
		Remote
	}

	class Program
	{
		static int Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			string webjobName = Environment.GetEnvironmentVariable("WEBJOBS_NAME");
			bool isRunningLocally = webjobName == null;
			if (isRunningLocally) // running locally
			{
				var program = new Program(isRunningLocally, StorageType.Local);
				return program.Run(args);
			}
			else
			{
				var configuration = new JobHostConfiguration(Environment.GetEnvironmentVariable("AzureWebJobsDashboard"));
				var host = new JobHost(configuration);
				// The following code ensures that the WebJob will be running continuously
				host.RunAndBlock();
				return 0;
			}
		}

		#region Fields, constructor

		SolutionHub solutionHub;
		ILogger logger;

		readonly bool isRunningLocally;
		readonly StorageType storageType;
		readonly Stopwatch stopwatch;

		public Program(bool isRunningLocally, StorageType storageType)
		{
			this.isRunningLocally = isRunningLocally;
			this.storageType = storageType;
			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		#endregion

		#region Public methods

		public int Run(IEnumerable<string> args)
		{
			int code;
			try
			{
				code = Parser.Default.ParseArguments<ImportOptions, FilterOptions, CompressOptions, DiffOptions, ListOptions>(args)
					.WithParsed((Options options) => initializeSolutionHub(options))
					.MapResult(
						(ImportOptions options) => runImportCommand(options),
						(FilterOptions options) => runFilterCommand(options),
						(CompressOptions options) => runCompressCommand(options),
						(DiffOptions options) => runDiffCommand(options),
						(ListOptions options) => runListCommand(options),
						errors => 1);
			}
			catch (Exception ex)
			{
				logger?.LogError(ex);
				code = 1;
			}
			goodBye(success: code == 0);
			return code;
		}

		#endregion

		#region Commands

		private int runImportCommand(ImportOptions options)
		{
			solutionHub.Import(options.ProjectName, options.MeshFile, options.ResultFiles);
			return 0;
		}

		private int runFilterCommand(FilterOptions options)
		{
			solutionHub.Filter(options.ParentLayer, options.FilterType, options.FilterParameters, options.LayerName);
			return 0;
		}

		private int runCompressCommand(CompressOptions options)
		{
			solutionHub.Compress(options.Layer, options.Method, options.FieldName, options.ComponentName);
			return 0;
		}

		private int runDiffCommand(DiffOptions options)
		{
			solutionHub.Diff(options.Layer);
			return 0;
		}

		private int runListCommand(ListOptions options)
		{
			foreach (var layerInfo in solutionHub.EnumerateAllLayers())
			{
				printLayerInfo(layerInfo, depth: 1);
			}
			return 0;
		}

		#endregion

		#region Private methods

		private void initializeSolutionHub(Options options)
		{
			logger = new ConsoleLogger(options.Verbose);

			if (!isRunningLocally && !options.SolutionId.HasValue)
			{
				throw new ArgumentNullException(nameof(options.SolutionId));
			}

			StorageType storageType = options.ForceUseRemoteStorage ? StorageType.Remote : this.storageType;

			switch (storageType)
			{
				case StorageType.Local:
					solutionHub = SolutionHub.CreateLocal(options.SolutionId ?? chooseSolution(storageType), logger);
					break;
				case StorageType.Remote:
					solutionHub = SolutionHub.CreateRemote(options.SolutionId ?? chooseSolution(storageType), logger);
					break;
				default:
					throw new NotSupportedException();
			}
		}

		private int chooseSolution(StorageType storageType)
		{
			ISolutionInfo[] solutions;
			switch (storageType)
			{
				case StorageType.Local:
					solutions = SolutionHub.EnumerateAllLocalSolutions(logger).ToArray();
					break;
				case StorageType.Remote:
					solutions = SolutionHub.EnumerateAllRemoteSolutions(logger).ToArray();
					break;
				default:
					throw new NotSupportedException();
			}

			if (solutions.Length == 0)
				throw new FileNotFoundException("No solution found");
			if (solutions.Length == 1)
				return solutions[0].Id;

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
					return id;
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

		private void printLayerInfo(ILayerInfo layerInfo, int depth)
		{
			logger.LogMessage($"{new string(' ', depth * 2)}+ '{layerInfo.Name}', filter: {layerInfo.FilterType}, {layerInfo.Id}", LogMessagePriority.High);
			foreach (var child in layerInfo.Children)
			{
				printLayerInfo(child, depth + 1);
			}
		}

		#endregion
	}
}
