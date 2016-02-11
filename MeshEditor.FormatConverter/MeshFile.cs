using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.FormatConverter
{
	class MeshFile
	{
		public Guid LayerId { get; set; }
		public double[] PointCoordinates { get; set; }
		public int[] EdgeConnectivity { get; set; }
		public int[] TriangleConnectivity { get; set; }
	}
}
