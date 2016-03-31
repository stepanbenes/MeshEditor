using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
	public class SVDCompressionService : ICompressionService
	{
		public double[] Compress(double[] dataValues, out CompressionParameters parameters)
		{
			parameters = new CompressionParameters
			{
				Method = CompressionMethod.SVD
			};

			// TODO: add reference to MathNet.Numerics, use Svd factorization, linearize vectors in matrice U, V, S to double array

			throw new NotImplementedException();
		}

		public double[] Decompress(double[] compressedData, CompressionParameters parameters)
		{
			Debug.Assert(parameters != null);
			Debug.Assert(parameters.Method == CompressionMethod.SVD);

			// TODO: create matrices U, V, S from compressedData, multiply them, linearize result to double array

			throw new NotImplementedException();
		}
	}
}
