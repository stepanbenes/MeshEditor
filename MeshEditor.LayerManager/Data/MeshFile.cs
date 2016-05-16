using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MeshEditor.LayerManager.Data
{
	public class MeshFile
	{
		public Guid LayerId { get; set; }

		public int Index { get; set; }

		public int NumberOfPoints { get; set; }
		public int NumberOfCells { get; set; }
		public int NumberOfEdges { get; set; }

		public float[] Center { get; set; }
		public float Radius { get; set; }

		public string PointCoordinates { get; set; }


		public string CellConnectivity { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string CellTypes { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string EdgeConnectivity { get; set; }
	}
}
