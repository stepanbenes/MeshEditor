using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.DataVisualizer
{
	public struct VectorIndex : IEquatable<VectorIndex>
	{
		public int X { get; }
		public int Y { get; }
		public int Z { get; }

		public VectorIndex(int x, int y, int z) => (X, Y, Z) = (x, y, z);

		public void Deconstruct(out int x, out int y, out int z)
		{
			x = X;
			y = Y;
			z = Z;
		}

		public IEnumerable<int> AllIndices()
		{
			yield return X;
			yield return Y;
			yield return Z;
		}

		public bool Equals(VectorIndex other) => X == other.X && Y == other.Y && Z == other.Z;

		public override bool Equals(object obj) => obj is VectorIndex other && this.Equals(other);

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + X.GetHashCode();
				hash = hash * 23 + Y.GetHashCode();
				hash = hash * 23 + Z.GetHashCode();
				return hash;
			}
		}

		public static bool operator ==(VectorIndex left, VectorIndex right) => left.Equals(right);
		public static bool operator !=(VectorIndex left, VectorIndex right) => !left.Equals(right);
	}
}
