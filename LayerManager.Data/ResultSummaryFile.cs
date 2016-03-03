using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayerManager.Types
{
	public class ResultSummaryFile
	{
		public Guid LayerId { get; set; }
		public double[] TimeSteps { get; set; }
		public ResultDescriptor[] ResultDescriptors { get; set; }
	}
}
