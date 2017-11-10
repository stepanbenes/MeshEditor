using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public struct EdgeIntersection : IEquatable<EdgeIntersection>
	{
		public int FirstPointId { get; }
		public int SecondPointId { get; }
		public float Coordinate { get; }

		public EdgeIntersection(int firstPointId, int secondPointId, float intersectionCoordinate)
		{
			Debug.Assert(firstPointId != secondPointId);
			Debug.Assert(intersectionCoordinate >= 0f && intersectionCoordinate <= 1f);
			
			// normalize
			if (intersectionCoordinate <= 0.5f)
			{
				FirstPointId = firstPointId;
				SecondPointId = secondPointId;
				Coordinate = intersectionCoordinate;
			}
			else
			{
				FirstPointId = secondPointId;
				SecondPointId = firstPointId;
				Coordinate = 1f - intersectionCoordinate;
			}
		}

		#region Equality

		private const float coordinateEpsilon = 1e-4f; // used in test of equivalence of two edge intersections

		public bool Equals(EdgeIntersection other) => this.FirstPointId == other.FirstPointId && (this.SecondPointId == other.SecondPointId || (this.Coordinate <= coordinateEpsilon && other.Coordinate <= coordinateEpsilon));

		public override bool Equals(object obj) => obj is EdgeIntersection e && this.Equals(e);

		public override int GetHashCode() => FirstPointId; // If two objects compare as equal, the GetHashCode method for each object must return the same value.

		#endregion
	}
}
