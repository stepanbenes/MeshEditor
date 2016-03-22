using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Filters;

namespace MeshEditor.LayerManager.Data
{
	public class SummaryLayerFile
	{
		public Guid Id { get; set; }
		public string Name { get; set; }

		public Guid? ParentId { get; set; }
		public FilterDescriptor[] Filters { get; set; }

		public double[] TimeSteps { get; set; }
		public ResultDescriptor[] Results { get; set; }
	}
}
