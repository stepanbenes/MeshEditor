using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Construction
{
	/// <summary>
	/// trida pro reprezentaci informaci o trojuhelnikove plose; pouzije se pri nacitani plochy ze souboru
	/// </summary>
	public struct TriangleMark
	{

		#region Fields, Constructor

		private int node1ID, node2ID, node3ID;

		public TriangleMark(int n1, int n2, int n3)
		{
			// sort indices
			int[] array = new int[] { n1, n2, n3 };
			Array.Sort<int>(array);
			this.node1ID = array[0];
			this.node2ID = array[1];
			this.node3ID = array[2];
			//if (n1 <= n2 && n1 <= n3)
			//{
			//    if (n2 <= n3)
			//    {
			//        node1ID = n1;
			//        node2ID = n2;
			//        node3ID = n3;
			//    }
			//    else
			//    {
			//        node1ID = n1;
			//        node2ID = n3;
			//        node3ID = n2;
			//    }
			//}
			//else if (n2 <= n1 && n2 <= n3)
			//{
			//    if (n1 <= n3)
			//    {
			//        node1ID = n2;
			//        node2ID = n1;
			//        node3ID = n3;
			//    }
			//    else
			//    {
			//        node1ID = n2;
			//        node2ID = n3;
			//        node3ID = n1;
			//    }
			//}
			//else //if (n3 <= n1 && n3 <= n2)
			//{
			//    if (n1 <= n2)
			//    {
			//        node1ID = n3;
			//        node2ID = n1;
			//        node3ID = n2;
			//    }
			//    else
			//    {
			//        node1ID = n3;
			//        node2ID = n2;
			//        node3ID = n1;
			//    }
			//}
		}

		#endregion

		#region Access properties

		public int Node1ID { get { return node1ID; } }
		public int Node2ID { get { return node2ID; } }
		public int Node3ID { get { return node3ID; } }

		#endregion

		#region Comparison members

		public static bool operator ==(TriangleMark a, TriangleMark b)
		{
			return a.node1ID == b.node1ID && a.node2ID == b.node2ID && a.node3ID == b.node3ID;
		}

		public static bool operator !=(TriangleMark a, TriangleMark b)
		{
			return !(a == b);
		}

		public bool Equals(TriangleMark other)
		{
			return this == other;
		}

		public override bool Equals(object obj)
		{
			return this == (TriangleMark)obj;
		}

		public override int GetHashCode()
		{
			return this.node1ID; /**/
		}

		#endregion

		#region ToString

		public override string ToString()
		{
			return "3 " + node1ID + " " + node2ID + " " + node3ID;
		}

		#endregion
	}
}
