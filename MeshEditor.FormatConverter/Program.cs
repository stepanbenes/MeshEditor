using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Globalization;
using CommandLine;
using System.Collections.Generic;
using System.Diagnostics;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.IO;
using Microsoft.Azure.WebJobs;
using MeshEditor.LayerManager.Common;

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
				
				var program = new Program(isRunningLocally, storageType: StorageType.Local, logger: new ConsoleLogger());

				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				try
				{
					returnCode = program.Run(args);
				}
				catch (Exception ex)
				{
					using (new ConsoleBrush(ConsoleColor.Red))
					{
						Console.Error.WriteLine(ex.GetType().FullName);
						Console.Error.WriteLine(ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
					}
					returnCode = -1;
				}

				stopwatch.Stop();

				if (returnCode != 1)
				{
					if (returnCode == 0)
					{
						using (new ConsoleBrush(ConsoleColor.Green))
							Console.Write("Success. ");
					}
					else
					{
						using (new ConsoleBrush(ConsoleColor.Red))
							Console.Write("Fail. ");
					}

					using (new ConsoleBrush(ConsoleColor.Gray))
						Console.WriteLine($"Execution time: {stopwatch.Elapsed}");
				}

				if (args.Any(arg => string.Equals(arg, "--pressanykey", StringComparison.InvariantCultureIgnoreCase)))
				{
					Console.WriteLine();
					using (new ConsoleBrush(ConsoleColor.Yellow))
						Console.Write("Press any key to quit...");
					Console.ReadKey();
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

		public Program(bool isRunningLocally, StorageType storageType, ILogger logger)
		{
			this.isRunningLocally = isRunningLocally;
			this.storageType = storageType;
			this.logger = logger;
		}

		#endregion

		#region Public methods

		public int Run(IEnumerable<string> args)
		{
			return Parser.Default.ParseArguments<CreateOptions, ImportOptions, FilterOptions, CompressOptions, ListOptions, DeleteOptions>(args)
					.WithParsed((Options options) => initializeSolutionHub(options))
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
			solutionHub.Import(options.AnalysisResultGroupLengths, options.AnalysisResultRecordNames, options.KeyTimeSteps, options.CompressionParameters, options.GaussPointsExtrapolationStrategyName, options.FieldName, options.LayerName);
			return 0;
		}

		private int runFilterCommand(FilterOptions options)
		{
			solutionHub.Filter(options.ParentLayer, options.FilterType, options.FilterParameters, options.KeyTimeSteps, options.CompressionParameters, options.FieldName, options.LayerName);
			return 0;
		}

		private int runCompressCommand(CompressOptions options)
		{
			solutionHub.Compress(options.Layer, options.KeyTimeSteps, options.CompressionParameters, options.FieldName, options.LayerName);
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

		private void initializeSolutionHub(Options options)
		{
			if (logger != null)
			{
				logger.VerbosityLevel = options.Verbose ? LogVerbosityLevel.All : LogVerbosityLevel.Message;
			}

			if (!isRunningLocally && !options.SolutionId.HasValue)
			{
				throw new ArgumentNullException(nameof(options.SolutionId));
			}

			StorageType storageType = options.ForceUseRemoteStorage ? StorageType.Remote : this.storageType;

			switch (storageType)
			{
				case StorageType.Local:
					{
						string solutionDirectory = options.SolutionDirectory ?? SolutionHub.GetLocalStorageDefaultDirectory();
						int solutionId = options.SolutionId ?? chooseSolution(SolutionHub.EnumerateAllLocalSolutions(solutionDirectory, logger).ToArray());
						solutionHub = SolutionHub.CreateLocal(solutionId, solutionDirectory, logger);
					}
					break;
				case StorageType.Remote:
					{
						int solutionId = options.SolutionId ?? chooseSolution(SolutionHub.EnumerateAllRemoteSolutions(logger).ToArray());
						solutionHub = SolutionHub.CreateRemote(solutionId, logger);
					}
					break;
				default:
					throw new NotSupportedException();
			}
		}

		private int chooseSolution(IReadOnlyList<ISolutionInfo> solutions)
		{
			Debug.Assert(isRunningLocally);

			if (solutions.Count == 0)
				throw new FileNotFoundException("No solution found");
			if (solutions.Count == 1)
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
			logger.LogMessage($"{new string(' ', depth * 2)}+ '{layerInfo.Name}', filter: {layerInfo.FilterType}, {layerInfo.Id}");
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

			using (new ConsoleBrush(ConsoleColor.Cyan))
			{
				Console.WriteLine(excavators[new Random().Next(excavators.Length)]);
			}
		}

		#endregion
	}
}
