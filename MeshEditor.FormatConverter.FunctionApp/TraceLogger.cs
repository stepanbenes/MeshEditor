using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common.Logging;
using Microsoft.Azure.WebJobs.Host;
using System.Diagnostics;

namespace MeshEditor.FormatConverter.FunctionApp
{
	class TraceLogger : ILogger
	{
		readonly TraceWriter log;

		public TraceLogger(TraceWriter log)
		{
			this.log = log;
		}

		public TraceLevel Level
		{
			get => log.Level;
			set => log.Level = value;
		}

		public void LogError(string message)
		{
			log.Error(message);
		}

		public void LogMessage(string message)
		{
			log.Info(message);
		}

		public void LogOperationProgress(string message)
		{
			log.Trace(new TraceEvent(TraceLevel.Verbose, message));
		}

		public void LogWarning(string message)
		{
			log.Warning(message);
		}
	}
}
