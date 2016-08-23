using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Common;

namespace MeshEditor.DataVisualizer.Services
{
	class MemoryLogger : ILogger
	{
		public enum RecordType
		{
			OperationProgress,
			Message,
			Warning,
			Error
		}

		public struct Record
		{
			public DateTime When { get; }
			public RecordType Type { get; }
			public string Content { get; }
			public Record(RecordType type, string content)
			{
				When = DateTime.Now; // or UtcNow?
				Type = type;
				Content = content;
			}

			public override string ToString() => $"{When}: [{Type}] {Content}";
		}

		List<Record> recordHistory = new List<Record>();

		public LogVerbosityLevel VerbosityLevel { get; set; } = LogVerbosityLevel.All;

		public IReadOnlyCollection<Record> GetRecordHistory() => new ReadOnlyCollection<Record>(recordHistory);

		public void LogOperationProgress(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.OperationProgress)
			{
				recordHistory.Add(new Record(RecordType.OperationProgress, message));
			}
		}

		public void LogMessage(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Message)
			{
				recordHistory.Add(new Record(RecordType.Message, message));
			}
		}

		public void LogWarning(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Warning)
			{
				recordHistory.Add(new Record(RecordType.Warning, message));
			}
		}

		public void LogError(string message)
		{
			if (VerbosityLevel >= LogVerbosityLevel.Error)
			{
				recordHistory.Add(new Record(RecordType.Error, message));
			}
		}
	}
}
