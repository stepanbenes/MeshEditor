using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common.Logging
{
	public enum LogVerbosityLevel
	{
		Nothing = 0,
		Error = 1,
		Warning = 2,
		Message = 3,
		OperationProgress = 4,
		All = 5
	}
}
