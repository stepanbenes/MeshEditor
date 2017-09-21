using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using MeshEditor.SolutionManager.CommandLine;
using MeshEditor.SolutionManager.Logging;

namespace MeshEditor.FormatConverter
{
	static class Program
	{
		public static int Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			if (args == null || args.Length == 0)
			{
				drawHelloImage();
			}

			int returnCode = 1;

			var parser = new CommandLineParser(isRunningLocally: true, storageType: StorageType.Local, logger: new ConsoleLogger());

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			try
			{
				returnCode = parser.Run(args);
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
		
		private static void drawHelloImage()
		{
			// taken from: http://ascii.co.uk/art/excavator
			string[] excavators = {
@"
	 --.
  ._// <>
  |_|_
 (o___o)",
@"
   //\\  ___          
   Y  \\/_/=| 
  _L  ((|_L_| 
 (/\)(__(____)",
@"
	 __
	//\\`'-.___
   //  \\  _(=()__
   Y    \\//~//.--|
   :    /\\~~//_  |
  _L   |_((_|___L_|
 (/\) (____(_______)",
			};

			using (new ConsoleBrush(ConsoleColor.Cyan))
			{
				Console.WriteLine(excavators[new Random().Next(excavators.Length)]
#if DEBUG
					+ " DEBUGGING..."
#endif
					+ Environment.NewLine
					);
			}
		}
	}
}
