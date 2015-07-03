using MeshEditor.Data;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.Cuts
{
	public struct EdgeIntersection : IEquatable<EdgeIntersection>
	{
		public Node Node1, Node2;
		public float T; // intersection parameter in range <0,1>

		public EdgeIntersection(Node node1, Node node2, float t)
		{
			Debug.Assert(node1 != null && node2 != null);
			Debug.Assert(t >= 0f && t <= 1f);
			this.Node1 = node1;
			this.Node2 = node2;
			this.T = t;
		}

		public Vector3 GetIntersection()
		{
			Vector3 v1 = Node1.Position;
			Vector3 v2 = Node2.Position;
			Vector3 result;
			Vector3.Subtract(ref v2, ref v1, out result);
			Vector3.Multiply(ref result, T, out result);
			Vector3.Add(ref v1, ref result, out result);
			return result;
		}

		public bool Equals(EdgeIntersection other)
		{
			return (this.Node1 == other.Node1 && this.Node2 == other.Node2) || (this.Node1 == other.Node2 && this.Node2 == other.Node1);
		}

		#region Overrides

		public override bool Equals(object obj)
		{
			if (!(obj is EdgeIntersection))
				return false;
			return this.Equals((EdgeIntersection)obj);
		}

		public override int GetHashCode()
		{
			//return Math.Min(Node1.ID, Node2.ID);
			return this.Node1.ID ^ this.Node2.ID;
		}

		public override string ToString()
		{
			return string.Format("Node1ID: {0} Node2ID: {1} T: {3:G3}", Node1.ID, Node2.ID, T);
		}

		#endregion

	}
}
