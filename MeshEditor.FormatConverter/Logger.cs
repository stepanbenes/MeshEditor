using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.Logging;

namespace MeshEditor.FormatConverter
{
	class Logger : ILogger
	{
		TextWriter log;

		public bool Verbose { get; }

		public Logger(TextWriter log, bool verbose)
		{
			this.log = log;
			Verbose = verbose;
		}

		public void LogMessage(string message, LogMessagePriority priority = LogMessagePriority.Normal)
		{
			if (Verbose || priority == LogMessagePriority.High)
			{
				log.WriteLine(message);
			}
		}
	}
}
