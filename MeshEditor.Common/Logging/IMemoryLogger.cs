using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common.Logging
{
	public interface IMemoryLogger : ILogger
	{
		event EventHandler<LogRecordEventArgs> LogRecordReported;

		IReadOnlyCollection<LogRecord> GetRecordHistory();
	}
}
