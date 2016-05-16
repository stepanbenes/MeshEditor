using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class MeshFileDescriptor
	{
		public int Index { get; set; }

		public double[] TimeSteps { get; set; }

		public DataFileDescriptor[] Attributes { get; set; }
		public DataFileDescriptor[] Results { get; set; }
	}
}
