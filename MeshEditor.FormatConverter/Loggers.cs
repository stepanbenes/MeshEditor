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

		public static void WriteColored(string message, ConsoleColor color)
		{
			var previousConsoleColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.Write(message);
			Console.ForegroundColor = previousConsoleColor;
		}

		public static void WriteLineColored(string message, ConsoleColor color)
		{
			WriteColored(message, color);
			Console.WriteLine();
		}

		public void LogOperationProgress(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.OperationProgress)
			{
				WriteLineColored(message, ConsoleColor.Gray);
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
				WriteLineColored("WARNING: " + message, ConsoleColor.Yellow);
			}
		}

		public void LogError(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Error)
			{
				WriteLineColored("ERROR: " + message, ConsoleColor.Red);
			}
		}
	}
}
