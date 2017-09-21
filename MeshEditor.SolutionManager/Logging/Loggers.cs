using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.Common.Logging;
using MeshEditor.SolutionManager.CommandLine;
using System.Diagnostics;

namespace MeshEditor.SolutionManager.Logging
{
	public class TextLogger : ILogger
	{
		readonly TextWriter log;

		public TraceLevel Level { get; set; } = TraceLevel.Info;

		public TextLogger(TextWriter log)
		{
			this.log = log;
		}

		public void LogOperationProgress(string message)
		{
			if (Level >= TraceLevel.Verbose)
			{
				log.WriteLine(message);
			}
		}

		public void LogMessage(string message)
		{
			if (Level >= TraceLevel.Info)
			{
				log.WriteLine(message);
			}
		}

		public void LogWarning(string message)
		{
			if (Level >= TraceLevel.Warning)
			{
				log.WriteLine("WARNING: " + message);
			}
		}

		public void LogError(string message)
		{
			if (Level >= TraceLevel.Error)
			{
				log.WriteLine("ERROR: " + message);
			}
		}
	}

	public class ConsoleLogger : ILogger
	{
		public TraceLevel Level { get; set; } = TraceLevel.Info;

		public void LogOperationProgress(string message)
		{
			if (Level >= TraceLevel.Verbose)
			{
				using (new ConsoleBrush(ConsoleColor.Gray))
				{
					Console.WriteLine(message);
				}
			}
		}

		public void LogMessage(string message)
		{
			if (Level >= TraceLevel.Info)
			{
				Console.WriteLine(message);
			}
		}

		public void LogWarning(string message)
		{
			if (Level >= TraceLevel.Warning)
			{
				using (new ConsoleBrush(ConsoleColor.Yellow))
				{
					Console.WriteLine("WARNING: " + message);
				}
			}
		}

		public void LogError(string message)
		{
			if (Level >= TraceLevel.Error)
			{
				using (new ConsoleBrush(ConsoleColor.Red))
				{
					Console.Error.WriteLine("ERROR: " + message);
				}
			}
		}
	}
}
