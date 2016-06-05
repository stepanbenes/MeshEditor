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

			//string webjobName = Environment.GetEnvironmentVariable("WEBJOBS_NAME");
			//bool isRunningLocally = webjobName == null;

			string dashboardAndStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsDashboard");
			bool isRunningLocally = dashboardAndStorageConnectionString == null;

			if (isRunningLocally) // running locally
			{
				if (args == null || args.Length == 0)
				{
					drawHelloImage();
				}

				int returnCode = 1;
				Stopwatch stopwatch = new Stopwatch();
				var program = new Program(isRunningLocally, StorageType.Local);
				stopwatch.Start();
				try
				{
					returnCode = program.Run(args, Console.Out);
				}
				catch (Exception ex)
				{
					var color = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"{ex.GetType().FullName}");
					Console.WriteLine($"{ex.Message + Environment.NewLine + Environment.NewLine} {ex.StackTrace}");
					Console.ForegroundColor = color;
					returnCode = -1;
				}
				finally
				{
					stopwatch.Stop();
					if (returnCode != 1)
					{
						var color = Console.ForegroundColor;
						if (returnCode == 0)
						{
							Console.ForegroundColor = ConsoleColor.Green;
							Console.Write("Success. ");
						}
						else
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.Write("Fail. ");
						}
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine($"Execution time: {stopwatch.Elapsed}");
						Console.ForegroundColor = color;
					}
				}
				return returnCode;
			}
			else
			{
				Debug.Assert(!string.IsNullOrEmpty(dashboardAndStorageConnectionString));
				var configuration = new JobHostConfiguration(dashboardAndStorageConnectionString);
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

		public Program(bool isRunningLocally, StorageType storageType)
		{
			this.isRunningLocally = isRunningLocally;
			this.storageType = storageType;
		}

		#endregion

		#region Public methods

		public int Run(IEnumerable<string> args, TextWriter log)
		{
			return Parser.Default.ParseArguments<CreateOptions, ImportOptions, FilterOptions, CompressOptions, ListOptions, DeleteOptions>(args)
					.WithParsed((Options options) => initializeSolutionHub(options, log))
					.MapResult(
						(CreateOptions options) => runCreateCommand(options),
						(ImportOptions options) => runImportCommand(options),
						(FilterOptions options) => runFilterCommand(options),
						(CompressOptions options) => runCompressCommand(options),
						(ListOptions options) => runListCommand(options),
						(DeleteOptions options) => runDeleteCommand(options),
						errors => 1);
		}

		#endregion

		#region Commands

		private int runCreateCommand(CreateOptions options)
		{
			solutionHub.Create(options.AnalysisResultGroupLengths, options.AnalysisResultRecordNames, options.ProjectName);
			return 0;
		}

		private int runImportCommand(ImportOptions options)
		{
			solutionHub.Import(options.AnalysisResultGroupLengths, options.AnalysisResultRecordNames);
			return 0;
		}

		private int runFilterCommand(FilterOptions options)
		{
			solutionHub.Filter(options.ParentLayer, options.FilterType, options.FilterParameters, options.LayerName);
			return 0;
		}

		private int runCompressCommand(CompressOptions options)
		{
			solutionHub.Compress(options.Layer, options.Method, options.KeyTimeSteps, options.FieldName, options.ComponentName, options.CompressionParameters);
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

		private int runDeleteCommand(DeleteOptions options)
		{
			solutionHub.Delete(options.Layer, options.DeleteAll);
			return 0;
		}

		#endregion

		#region Private methods

		private void initializeSolutionHub(Options options, TextWriter log)
		{
			logger = new Logger(log, options.Verbose);

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

		private void printLayerInfo(ILayerInfo layerInfo, int depth)
		{
			logger.LogMessage($"{new string(' ', depth * 2)}+ '{layerInfo.Name}', filter: {layerInfo.FilterType}, {layerInfo.Id}", LogMessagePriority.High);
			foreach (var child in layerInfo.Children)
			{
				printLayerInfo(child, depth + 1);
			}
		}

		private static void drawHelloImage()
		{
			// taken from: http://ascii.co.uk/art/excavator
			string[] excavators = {
@"
     --.
  ._// <>
  |_|_
 (o___o)
",
@"
   //\\  ___          
   Y  \\/_/=| 
  _L  ((|_L_| 
 (/\)(__(____)	 
",
@"
     __
    //\\`'-.___
   //  \\  _(=()__
   Y    \\//~//.--|
   :    /\\~~//_  |
  _L   |_((_|___L_|
 (/\) (____(_______)
",
			};

			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(excavators[new Random().Next(excavators.Length)]);
			Console.ForegroundColor = color;
		}

		#endregion
	}
}
