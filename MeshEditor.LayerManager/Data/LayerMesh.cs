using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class LayerMesh
	{
		public Guid LayerId { get; set; }
		public string PointCoordinates { get; set; }
		public string EdgeConnectivity { get; set; }
		public string TriangleConnectivity { get; set; }
	}
}
