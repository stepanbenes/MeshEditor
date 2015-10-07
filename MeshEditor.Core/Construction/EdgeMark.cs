using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Construction
{
	/// <summary>
	/// trida pro reprezentaci informaci o hrane; pouzije se pri nacitani hran ze souboru
	/// </summary>
	public struct EdgeMark
	{
		private int node1ID, node2ID;

		public EdgeMark(int value1, int value2)
		{
			if (value1 < value2)
			{
				node1ID = value1;
				node2ID = value2;
			}
			else
			{
				node1ID = value2;
				node2ID = value1;
			}
		}

		public int Node1ID
		{
			get { return node1ID; }
		}

		public int Node2ID
		{
			get { return node2ID; }
		}

		public bool Equals(EdgeMark value)
		{
			return (this.Node2ID == value.Node2ID && this.Node1ID == value.Node1ID);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is EdgeMark))
				return false;
			return this.Equals((EdgeMark)obj);
		}

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + Node1ID.GetHashCode();
				hash = hash * 23 + Node2ID.GetHashCode();
				return hash;
			}
		}

		public override string ToString()
		{
			return Node1ID.ToString() + " " + Node2ID;
		}
	}
}
