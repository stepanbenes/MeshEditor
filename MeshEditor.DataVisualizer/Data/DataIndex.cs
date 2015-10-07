using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	/// <summary>
	/// Immutable structure desribing currently displayed data index.
	/// Uses (something like) CloneToModify pattern.
	/// </summary>
	public struct DataIndex : IEquatable<DataIndex>
	{
		public static readonly DataIndex Zero = new DataIndex();

		int index;
		double time;

		public int Index { get { return index; } }
		public double Time { get { return time; } }

		public DataIndex(int index, double time)
		{
			this.index = index;
			this.time = time;
		}

		public DataIndex WithIndex(int index)
		{
			return new DataIndex(index, this.Time);
		}

		public DataIndex WithTime(double time)
		{
			return new DataIndex(this.Index, time);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is DataIndex))
				return false;
			return this.Equals((DataIndex)obj);
		}

		public bool Equals(DataIndex other)
		{
			return this.index == other.index && this.time == other.time;
		}

		public override string ToString()
		{
			return string.Format("[Index: {0} Time: {1}]", Index, Time);
		}

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + index.GetHashCode();
				hash = hash * 23 + time.GetHashCode();
				return hash;
			}
		}

		public static bool operator ==(DataIndex a, DataIndex b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(DataIndex a, DataIndex b)
		{
			return !a.Equals(b);
		}
	}
}
