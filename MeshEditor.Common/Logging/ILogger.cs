using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common.Logging
{
	public interface ILogger
	{
		TraceLevel Level { get; set; }

		void LogOperationProgress(string message);
		void LogMessage(string message);
		void LogWarning(string message);
		void LogError(string message);
	}
}
