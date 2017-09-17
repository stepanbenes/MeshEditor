using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.DataVisualizer
{
	public struct VectorIndex
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
	}
}
