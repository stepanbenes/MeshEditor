using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayerManager.Types
{
	public class MeshFile
	{
		public Guid LayerId { get; set; }
		public string PointCoordinates { get; set; }
		public string EdgeConnectivity { get; set; }
		public string TriangleConnectivity { get; set; }
	}
}
