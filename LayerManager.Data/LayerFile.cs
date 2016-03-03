using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayerManager.Types
{
	public class LayerFile
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public MeshFile MeshFile { get; set; }
		public ResultSummaryFile ResultSummaryFile { get; set; }
	}
}
