using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common;
using System.Diagnostics;

namespace MeshEditor.Common.Logging
{
	public class MemoryLogger : IMemoryLogger
	{
		readonly List<LogRecord> recordHistory = new List<LogRecord>();

		public TraceLevel Level { get; set; } = TraceLevel.Verbose;

		public event EventHandler<LogRecordEventArgs> LogRecordReported;

		public IReadOnlyList<LogRecord> GetRecordHistory() => new ReadOnlyCollection<LogRecord>(recordHistory);

		public void ClearHistory() => recordHistory.Clear();

		public void LogOperationProgress(string message)
		{
			if (Level >= TraceLevel.Verbose)
			{
				logRecord(new LogRecord(RecordType.OperationProgress, message));
			}
		}

		public void LogMessage(string message)
		{
			if (Level >= TraceLevel.Info)
			{
				logRecord(new LogRecord(RecordType.Message, message));
			}
		}

		public void LogWarning(string message)
		{
			if (Level >= TraceLevel.Warning)
			{
				logRecord(new LogRecord(RecordType.Warning, message));
			}
		}

		public void LogError(string message)
		{
			if (Level >= TraceLevel.Error)
			{
				logRecord(new LogRecord(RecordType.Error, message));
			}
		}

		private void logRecord(LogRecord record)
		{
			recordHistory.Add(record);
			LogRecordReported?.Invoke(this, new LogRecordEventArgs(record));
		}
	}
}
