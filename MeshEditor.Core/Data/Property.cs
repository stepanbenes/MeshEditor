using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Data
{
	/// <summary>
	/// vlastnost objektu (tuto datovou polozku obsahuji objekt typu Element, Node, WingedEdge, ...)
	/// </summary>
	public struct Property : IComparable, IComparable<Property>, IEquatable<Property>
	{
		private int value;

		public static readonly Property Zero = new Property(0);

		public bool IsZero
		{
			get { return value == 0; }
		}

		public int Value
		{
			get { return this.value; }
		}

		public Property(int value)
		{
			this.value = value;
		}

		public override string ToString()
		{
			return value.ToString();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Property))
				return false;
			return this.Equals((Property)obj);
		}

		public bool Equals(Property other)
		{
			return this.value == other.value;
		}

		public static bool operator ==(Property a, Property b)
		{
			return a.value == b.value;
		}

		public static bool operator !=(Property a, Property b)
		{
			return a.value != b.value;
		}

		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return CompareTo((Property)obj);
		}

		public int CompareTo(Property other)
		{
			return this.value.CompareTo(other.value);
		}

		#endregion
	}
}
