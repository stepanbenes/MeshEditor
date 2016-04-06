using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public struct EdgeIntersection
	{
		public int FirstPointId { get; }
		public int SecondPointId { get; }
		public float Coordinate { get; }
		public EdgeIntersection(int firstPointId, int secondPointId, float intersectionCoordinate)
		{
			FirstPointId = firstPointId;
			SecondPointId = secondPointId;
			Coordinate = intersectionCoordinate;
		}
	}
}
