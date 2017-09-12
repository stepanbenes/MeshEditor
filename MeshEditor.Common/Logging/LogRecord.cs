using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common.Logging
{
	public enum RecordType
	{
		OperationProgress,
		Message,
		Warning,
		Error
	}

	public struct LogRecord
	{
		public DateTime WhenUtc { get; }
		public RecordType Type { get; }
		public string Content { get; }
		public LogRecord(RecordType type, string content)
		{
			WhenUtc = DateTime.UtcNow;
			Type = type;
			Content = content;
		}

		public override string ToString() => $"{WhenUtc.ToLocalTime()}: [{Type}] {Content}";
	}
}
