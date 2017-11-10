using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MeshEditor.Common.Extensions;
using static MeshEditor.Common.HelperFunctions;

namespace MeshEditor.Common.GeometryMarkers
{
	/// <summary>
	/// trida pro reprezentaci informaci o ctyruhelnikove plose; pouzije se pri nacitani plochy ze souboru
	/// </summary>
	public struct QuadMark : IEquatable<QuadMark>
	{
		#region Fields, Constructor

		private int node1ID, node2ID, node3ID, node4ID;

		public QuadMark(int n1, int n2, int n3, int n4)
		{
			// sort indices using bubble-sort
			if (n1 > n2)
				Swap(ref n1, ref n2);
			if (n1 > n3)
				Swap(ref n1, ref n3);
			if (n1 > n4)
				Swap(ref n1, ref n4);
			if (n2 > n3)
				Swap(ref n2, ref n3);
			if (n2 > n4)
				Swap(ref n2, ref n4);
			if (n3 > n4)
				Swap(ref n3, ref n4);

			node1ID = n1;
			node2ID = n2;
			node3ID = n3;
			node4ID = n4;

			Debug.Assert(new[] { node1ID, node2ID, node3ID, node4ID }.IsSorted());
		}

		#endregion

		#region Access properties

		public int Node1ID { get { return node1ID; } }
		public int Node2ID { get { return node2ID; } }
		public int Node3ID { get { return node3ID; } }
		public int Node4ID { get { return node4ID; } }

		#endregion

		#region Comparison members

		public static bool operator ==(QuadMark a, QuadMark b) => a.Equals(b);

		public static bool operator !=(QuadMark a, QuadMark b) => !a.Equals(b);

		public bool Equals(QuadMark other)
			=> this.node1ID == other.node1ID && this.node2ID == other.node2ID && this.node3ID == other.node3ID && this.node4ID == other.node4ID;

		public override bool Equals(object obj) => obj is QuadMark q && this.Equals(q);

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + node1ID.GetHashCode();
				hash = hash * 23 + node2ID.GetHashCode();
				hash = hash * 23 + node3ID.GetHashCode();
				return hash;
			}
		}

		public bool IsCollapsedToTriangle(out TriangleMark collapsedTriangle)
		{
			// node ids must be sorted!
			if (node1ID == node2ID)
			{
				collapsedTriangle = new TriangleMark(node2ID, node3ID, node4ID); // quad is collapsed to triangle
				return true;
			}
			if (node2ID == node3ID)
			{
				collapsedTriangle = new TriangleMark(node1ID, node3ID, node4ID); // quad is collapsed to triangle
				return true;
			}
			if (node3ID == node4ID)
			{
				collapsedTriangle = new TriangleMark(node1ID, node2ID, node4ID); // quad is collapsed to triangle
				return true;
			}
			collapsedTriangle = default(TriangleMark);
			return false;
		}

		#endregion

		#region ToString

		public override string ToString() => $"4 {node1ID} {node2ID} {node3ID} {node4ID}";

		#endregion
	}
}
