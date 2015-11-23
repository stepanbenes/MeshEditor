using System;
using System.Collections.Generic;
using System.Text;
using static MeshEditor.Utilities.Functions;

namespace MeshEditor.Construction
{
	/// <summary>
	/// trida pro reprezentaci informaci o ctyruhelnikove plose; pouzije se pri nacitani plochy ze souboru
	/// </summary>
	public struct QuadMark
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
		}

		#endregion

		#region Access properties

		public int Node1ID { get { return node1ID; } }
		public int Node2ID { get { return node2ID; } }
		public int Node3ID { get { return node3ID; } }
		public int Node4ID { get { return node4ID; } }

		#endregion

		#region Comparison members

		public static bool operator ==(QuadMark a, QuadMark b)
		{
			return a.node1ID == b.node1ID && a.node2ID == b.node2ID && a.node3ID == b.node3ID && a.node4ID == b.node4ID;
		}

		public static bool operator !=(QuadMark a, QuadMark b)
		{
			return !(a == b);
		}

		public bool Equals(QuadMark other)
		{
			return this == other;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is QuadMark))
				return false;
			return this == (QuadMark)obj;
		}

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

		public override string ToString()
		{
			return "4 " + node1ID + " " + node2ID + " " + node3ID + " " + node4ID;
		}

		#endregion
	}
}
