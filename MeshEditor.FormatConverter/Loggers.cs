using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Common;

namespace MeshEditor.FormatConverter
{
	class Logger : ILogger
	{
		TextWriter log;

		public LogVerbosityLevel VerbosityLevel { get; set; }

		public Logger(TextWriter log)
		{
			this.log = log;
		}

		public void LogOperationProgress(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.OperationProgress)
			{
				log.WriteLine(message);
			}
		}

		public void LogMessage(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Message)
			{
				log.WriteLine(message);
			}
		}

		public void LogWarning(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Warning)
			{
				log.WriteLine("WARNING: " + message);
			}
		}

		public void LogError(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Error)
			{
				log.WriteLine("ERROR: " + message);
			}
		}
	}

	class ConsoleLogger : ILogger
	{
		public LogVerbosityLevel VerbosityLevel { get; set; }

		public void LogOperationProgress(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.OperationProgress)
			{
				using (new ConsoleBrush(ConsoleColor.Gray))
				{
					Console.WriteLine(message);
				}
			}
		}

		public void LogMessage(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Message)
			{
				Console.WriteLine(message);
			}
		}

		public void LogWarning(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Warning)
			{
				using (new ConsoleBrush(ConsoleColor.Yellow))
				{
					Console.WriteLine("WARNING: " + message);
				}
			}
		}

		public void LogError(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Error)
			{
				using (new ConsoleBrush(ConsoleColor.Red))
				{
					Console.Error.WriteLine("ERROR: " + message);
				}
			}
		}
	}
}
