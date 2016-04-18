using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.Logging;

namespace MeshEditor.FormatConverter
{
	class ConsoleLogger : ILogger
	{
		public bool Verbose { get; }

		public ConsoleLogger(bool verbose)
		{
			Verbose = verbose;
		}

		public void LogMessage(string message, LogMessagePriority priority = LogMessagePriority.Normal)
		{
			if (Verbose || priority == LogMessagePriority.High)
			{
				Console.WriteLine(message);
			}
		}
	}
}
