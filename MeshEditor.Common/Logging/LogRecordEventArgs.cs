using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common.Logging
{
	public class LogRecordEventArgs : EventArgs
	{
		public LogRecord LogRecord { get; }
		public LogRecordEventArgs(LogRecord logRecord) => LogRecord = logRecord;
	}
}
