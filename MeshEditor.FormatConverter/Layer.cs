using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.FormatConverter
{
	class Layer
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public double[] PointCoordinates { get; set; }
		public int[] EdgeConnectivity { get; set; }
		public int[] TriangleConnectivity { get; set; }
	}
}
