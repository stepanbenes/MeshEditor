using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.Logging
{
	public interface ILogger
	{
		void LogMessage(string message, LogMessagePriority priority = LogMessagePriority.Normal);
		void LogError(Exception ex);
	}
}
