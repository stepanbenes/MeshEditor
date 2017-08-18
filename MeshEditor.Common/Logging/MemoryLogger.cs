using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common;

namespace MeshEditor.Common.Logging
{
	public class MemoryLogger : IMemoryLogger
	{
		readonly List<LogRecord> recordHistory = new List<LogRecord>();

		public LogVerbosityLevel VerbosityLevel { get; set; } = LogVerbosityLevel.All;

		public event EventHandler<LogRecordEventArgs> LogRecordReported;

		public IReadOnlyCollection<LogRecord> GetRecordHistory() => new ReadOnlyCollection<LogRecord>(recordHistory);

		public void LogOperationProgress(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.OperationProgress)
			{
				logRecord(new LogRecord(RecordType.OperationProgress, message));
			}
		}

		public void LogMessage(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Message)
			{
				logRecord(new LogRecord(RecordType.Message, message));
			}
		}

		public void LogWarning(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Warning)
			{
				logRecord(new LogRecord(RecordType.Warning, message));
			}
		}

		public void LogError(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Error)
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
