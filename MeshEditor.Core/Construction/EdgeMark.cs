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
		public int Node2ID;
		public int Node1ID;

		public EdgeMark(int value1, int value2)
		{
			if (value1 < value2)
			{
				Node1ID = value1;
				Node2ID = value2;
			}
			else
			{
				Node1ID = value2;
				Node2ID = value1;
			}
		}

		public bool Equals(EdgeMark value)
		{
			return (this.Node2ID == value.Node2ID && this.Node1ID == value.Node1ID);
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;
			return this.Equals((EdgeMark)obj);
		}

		public override int GetHashCode()
		{
			return this.Node1ID; /**/
		}

		public override string ToString()
		{
			return Node1ID.ToString() + " " + Node2ID;
		}
	}
}
