using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Filters;

namespace MeshEditor.LayerManager.Data
{
	public class SummaryFile
	{
		public Guid Id { get; set; }
		public string Name { get; set; }

		public Guid? ParentId { get; set; }
		public Filter Filter { get; set; }

		public double[] TimeSteps { get; set; }

		public DataFileDescriptor[] Attributes { get; set; }
		public DataFileDescriptor[] Results { get; set; }
	}
}
