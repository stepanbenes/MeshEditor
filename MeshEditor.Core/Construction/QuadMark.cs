using System;
using System.Collections.Generic;
using System.Text;

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
			// sort indices
			int[] array = new int[] { n1, n2, n3, n4 };
			Array.Sort<int>(array);
			this.node1ID = array[0];
			this.node2ID = array[1];
			this.node3ID = array[2];
			this.node4ID = array[3];
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
			return this == (QuadMark)obj;
		}

		public override int GetHashCode()
		{
			return this.node1ID; /**/
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
