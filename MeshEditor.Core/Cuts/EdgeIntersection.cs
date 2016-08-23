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
		private Node node1, node2;
		private float t; // intersection parameter in range <0,1>

		public EdgeIntersection(Node node1, Node node2, float t)
		{
			Debug.Assert(node1 != null && node2 != null);
			Debug.Assert(t >= 0f && t <= 1f);
			this.node1 = node1;
			this.node2 = node2;
			this.t = t;
		}

		public Node Node1
		{
			get { return node1; }
		}

		public Node Node2
		{
			get { return node2; }
		}

		public float T
		{
			get
			{
				return t;
			}
		}

		public Vector3 GetIntersection()
		{
			Vector3 v1 = node1.Position;
			Vector3 v2 = node2.Position;
			Vector3 result;
			Vector3.Subtract(ref v2, ref v1, out result);
			Vector3.Multiply(ref result, T, out result);
			Vector3.Add(ref v1, ref result, out result);
			return result;
		}

		public bool Equals(EdgeIntersection other)
		{
			return (this.node1 == other.node1 && this.node2 == other.node2) || (this.node1 == other.node2 && this.node2 == other.node1);
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
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + node1.GetHashCode();
				hash = hash * 23 + node2.GetHashCode();
				return hash;
			}
		}

		public static bool operator ==(EdgeIntersection a, EdgeIntersection b) => a.Equals(b);

		public static bool operator !=(EdgeIntersection a, EdgeIntersection b) => !a.Equals(b);

		public override string ToString() => string.Format("Node1ID: {0} Node2ID: {1} T: {2:G3}", node1.ID, node2.ID, t);

		#endregion

	}
}
