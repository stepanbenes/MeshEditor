using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MeshEditor.Common.Extensions;
using static MeshEditor.Common.HelperFunctions;

namespace MeshEditor.Common.GeometryMarkers
{
	/// <summary>
	/// trida pro reprezentaci informaci o trojuhelnikove plose; pouzije se pri nacitani plochy ze souboru
	/// </summary>
	public struct TriangleMark : IEquatable<TriangleMark>
	{

		#region Fields, Constructor

		private int node1ID, node2ID, node3ID;

		public TriangleMark(int n1, int n2, int n3)
		{
			// sort indices using bubble-sort
			if (n1 > n2)
				Swap(ref n1, ref n2);
			if (n1 > n3)
				Swap(ref n1, ref n3);
			if (n2 > n3)
				Swap(ref n2, ref n3);

			node1ID = n1;
			node2ID = n2;
			node3ID = n3;

			Debug.Assert(new[] { node1ID, node2ID, node3ID }.IsSorted());
		}

		#endregion

		#region Access properties

		public int Node1ID { get { return node1ID; } }
		public int Node2ID { get { return node2ID; } }
		public int Node3ID { get { return node3ID; } }

		#endregion

		#region Comparison members

		public static bool operator ==(TriangleMark a, TriangleMark b) => a.Equals(b);

		public static bool operator !=(TriangleMark a, TriangleMark b) => !a.Equals(b);

		public bool Equals(TriangleMark other) => this.node1ID == other.node1ID && this.node2ID == other.node2ID && this.node3ID == other.node3ID;

		public override bool Equals(object obj) => obj is TriangleMark t && this.Equals(t);

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

		#endregion

		#region ToString

		public override string ToString() => $"3 {node1ID} {node2ID} {node3ID}";

		#endregion
	}
}
