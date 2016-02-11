using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.FormatConverter
{
	class ResultSummaryFile
	{
		public Guid LayerId { get; set; }
		public ResultFile[] ResultFiles { get; set; }
	}
}
