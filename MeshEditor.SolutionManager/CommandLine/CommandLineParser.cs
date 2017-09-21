using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.Common.Logging;
using MeshEditor.SolutionManager.IO;
using CommandLine;
using System.IO;
using MeshEditor.Common.Extensions;

namespace MeshEditor.SolutionManager.CommandLine
{
	public enum StorageType
	{
		Local,
		Remote
	}

	public class CommandLineParser
	{
		#region Fields, constructor

		readonly bool isRunningLocally;
		readonly StorageType storageType;
		readonly ILogger logger;

		public CommandLineParser(bool isRunningLocally, StorageType storageType, ILogger logger)
		{
			this.isRunningLocally = isRunningLocally;
			this.storageType = storageType;
			this.logger = logger;
		}

		#endregion

		#region Public methods

		public int Run(string allArgs) => Run(allArgs.SplitToTokensWithQuotes());

		public int Run(IEnumerable<string> args)
		{
			return Parser.Default.ParseArguments<ImportOptions, FilterOptions, CompressOptions, ListOptions, DeleteOptions, DiffOptions>(args)
					.MapResult(
						(ImportOptions options) => runImportCommand(options),
						(FilterOptions options) => runFilterCommand(options),
						(CompressOptions options) => runCompressCommand(options),
						(ListOptions options) => runListCommand(options),
						(DeleteOptions options) => runDeleteCommand(options),
						(DiffOptions options) => runDiffCommand(options),
						errors => 1);
		}

		#endregion

		#region Commands

		private int runImportCommand(ImportOptions options)
		{
			var solutionHub = initializeSolutionHub(options);
			solutionHub.Import(options.KeyTimeSteps, options.CompressionParameters, options.GaussPointsExtrapolationStrategyName, options.FieldName, options.LayerName);
			return 0;
		}

		private int runFilterCommand(FilterOptions options)
		{
			var solutionHub = initializeSolutionHub(options);
			solutionHub.Filter(options.ParentLayer, options.FilterType, options.FilterParameters, options.KeyTimeSteps, options.CompressionParameters, options.FieldName, options.LayerName);
			return 0;
		}

		private int runCompressCommand(CompressOptions options)
		{
			var solutionHub = initializeSolutionHub(options);
			solutionHub.Compress(options.Layer, options.KeyTimeSteps, options.CompressionParameters, options.FieldName, options.LayerName);
			return 0;
		}

		private int runListCommand(ListOptions options)
		{
			var solutionHub = initializeSolutionHub(options);
			var layers = solutionHub.GetSolutionDescription().Layers;
			for (int i = 0; i < layers.Count; i++)
			{
				printLayerInfo(layers[i], "", isLast: i == layers.Count - 1);
			}
			return 0;
		}

		private int runDeleteCommand(DeleteOptions options)
		{
			var solutionHub = initializeSolutionHub(options);
			solutionHub.Delete(options.Layer, options.DeleteAll);
			return 0;
		}

		private int runDiffCommand(DiffOptions options)
		{
			var solutionHub = initializeSolutionHub(options);
			solutionHub.Diff(options.Layer);
			return 0;
		}

		#endregion

		#region Private methods

		private SolutionHub initializeSolutionHub(Options options)
		{
			if (logger != null && options.Verbose)
			{
				logger.Level = TraceLevel.Verbose;
			}

			int solutionId;
			if (!isRunningLocally && !int.TryParse(options.Solution, NumberStyles.Integer, CultureInfo.InvariantCulture, out solutionId))
			{
				throw new FormatException("Argument Solution is not an integer");
			}

			StorageType requestedStorageType = options.ForceUseRemoteStorage ? StorageType.Remote : this.storageType;

			switch (requestedStorageType)
			{
				case StorageType.Local:
					{
						if (options.Solution == null)
						{
							// first try to look in current directory (exclude sub-directories)
							var solutionDirectory = Directory.GetCurrentDirectory();
							var solutions = SolutionHub.EnumerateAllLocalSolutions(solutionDirectory, includeOneSubDirectory: true, logger: logger).ToArray();

#if DEBUG
							if (solutions.Length == 0) // if nothing found, try to look in default local storage
							{
								solutionDirectory = SolutionHub.GetLocalStorageDefaultDirectory();
								solutions = SolutionHub.EnumerateAllLocalSolutions(solutionDirectory, includeOneSubDirectory: true, logger: logger).ToArray();
							}
#endif

							var solutionIndex = chooseSolution(solutions);
							var solutionFileName = solutions[solutionIndex].Location;
							return SolutionHub.OpenLocal(solutionFileName, logger);
						}
						else // option.Solution should be solution file full path
						{
							return SolutionHub.OpenLocal(options.Solution, logger);
						}
					}
				case StorageType.Remote:
					{
						if (options.Solution == null)
						{
							var solutions = SolutionHub.EnumerateAllRemoteSolutions(logger).ToArray();
							var solutionIndex = chooseSolution(solutions);
							solutionId = solutions[solutionIndex].Id;
						}
						else
						{
							solutionId = int.Parse(options.Solution, CultureInfo.InvariantCulture);
						}
						return SolutionHub.OpenRemote(solutionId, logger);
					}
				default:
					throw new NotSupportedException();
			}
		}

		private int chooseSolution(IReadOnlyList<ISolutionInfo> solutions)
		{
			Debug.Assert(isRunningLocally);

			if (solutions.Count == 0)
				throw new FileNotFoundException("No solution found.");

			if (solutions.Count == 1)
				return solutions[0].Id;

			// otherwise show menu:
			Console.WriteLine("Choose solution:");
			for (int i = 0; i < solutions.Count; i++)
			{
				var solution = solutions[i];
				Console.WriteLine($"# {i}, Solution id: {solution.Id}, Project name: '{solution.ProjectName}', Location: '{solution.Location}'");
			}

			// read input from keyboard
			while (true)
			{
				Console.Write("Index = ");
				string input = Console.ReadLine();
				int index;
				if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
				{
					Console.WriteLine("Please insert valid integer value.");
					continue;
				}
				if (index < 0 || index >= solutions.Count)
				{
					Console.WriteLine($"Index '{index}' is out of range.");
					continue;
				}
				return index;
			}
		}

		private static void printLayerInfo(ILayerInfo layerInfo, string indentation, bool isLast)
		{
			using (new ConsoleBrush(ConsoleColor.Yellow))
				Console.Write($"{indentation}{(isLast ? '└' : '├')}─");
			using (new ConsoleBrush(ConsoleColor.Magenta))
				Console.Write(layerInfo.Name);
			using (new ConsoleBrush(ConsoleColor.DarkGreen))
				Console.Write(" " + layerInfo.FilterType);
			using (new ConsoleBrush(ConsoleColor.DarkCyan))
				Console.Write(" " + layerInfo.Id);
			Console.WriteLine();

			indentation += isLast ? @"  " : @"| ";

			var lastChild = layerInfo.Children.LastOrDefault();
			foreach (var child in layerInfo.Children)
			{
				printLayerInfo(child, indentation, isLast: ReferenceEquals(child, lastChild));
			}
		}

		#endregion
	}
}