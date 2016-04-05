using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Double;
using MeshEditor.LayerManager.Common;

namespace MeshEditor.LayerManager.Compression
{
	internal class SVDCompressionService : ICompressionService
	{
		#region Constructor, Fields

		private double? desiredCompressionFactor;

		public SVDCompressionService()
		{
			desiredCompressionFactor = null;
		}

		public SVDCompressionService(double desiredCompressionFactor)
		{
			Debug.Assert(desiredCompressionFactor >= 0.0);
			Debug.Assert(desiredCompressionFactor <= 1.0);
			this.desiredCompressionFactor = desiredCompressionFactor;
		}

		#endregion

		#region Public methods

		public double[] Compress(IEnumerable<double[]> dataValues, int rows, int columns, out CompressionParameters parameters)
		{
			// create matrix from input data values, replace possible NaN values with zeroes
			Matrix A = /* SparseMatrix ? */ DenseMatrix.OfRows(rows, columns, dataValues.Select(row => row.Select(value => double.IsNaN(value) ? 0.0 : value)));
			Debug.Assert(A.RowCount == rows);
			Debug.Assert(A.ColumnCount == columns);

			// use MathNet.Numerics' implementation of SVD factorization
			var svd = SVD.Create(A);
			
			//double factor = (double)rank * (rows + columns + 1) / (rows * columns);
			//if (rank == 0) // if rank is zero, matrix A is full of zeroes, so it enables ultimate compression
			//{
			//	return new double[0];
			//}

			parameters = new SVDCompressionParameters
			{
				Rows = rows,
				Columns = columns,
				Rank = Math.Min(rows, columns)
			};

			Debug.Assert(svd.U.RowCount == rows);
			Debug.Assert(svd.U.ColumnCount == rows);
			Debug.Assert(svd.S.Count == Math.Min(rows, columns));
			Debug.Assert(svd.VT.RowCount == columns);
			Debug.Assert(svd.VT.ColumnCount == columns);

			// linearize vectors in matrice U, V, S to double array
			double[] result = new double[svd.U.RowCount * svd.U.ColumnCount + svd.S.Count + svd.VT.RowCount * svd.VT.ColumnCount];

			double[] uColumnWise = svd.U.EnumerateColumns().SelectMany(column => column).ToArray(); // take newRank columns of U
			Debug.Assert(uColumnWise.Length == rows * rows);
			Array.Copy(uColumnWise, result, uColumnWise.Length);
			int offset = uColumnWise.Length;
			//uColumnWise = null;

			double[] sDiagonal = svd.S.ToArray(); // take newRank singular values
			Debug.Assert(sDiagonal.Length == Math.Min(rows, columns));
			Array.Copy(sDiagonal, 0, result, offset, sDiagonal.Length);
			offset += sDiagonal.Length;
			//sDiagonal = null;

			double[] vtColumnWise = svd.VT.EnumerateRows().SelectMany(row => row).ToArray(); // take newRank rows of VT
			Debug.Assert(vtColumnWise.Length == columns * columns);
			Array.Copy(vtColumnWise, 0, result, offset, vtColumnWise.Length);
			offset += vtColumnWise.Length;
			//vtColumnWise = null;
			Debug.Assert(offset == result.Length);
			
			return result;
		}

		public IEnumerable<double[]> Decompress(double[] compressedData, CompressionParameters parameters)
		{
			Debug.Assert(parameters is SVDCompressionParameters);
			Debug.Assert(parameters.Method == CompressionMethod.SVD);
			SVDCompressionParameters svdParameters = (SVDCompressionParameters)parameters;

			if (svdParameters.Rank == 0) // if rank is zero, return matrix full of zeroes
			{
				return Enumerable.Repeat(new double[svdParameters.Columns] /*array full of zeroes*/, svdParameters.Rows);
			}

			// create matrices U, S, VT from compressedData
			int uSize = svdParameters.Rows * svdParameters.Rows;
			int sSize = svdParameters.Rank;
			int vtSize = svdParameters.Columns * svdParameters.Columns;
			Debug.Assert(uSize + sSize + vtSize == compressedData.Length);

			Matrix U = DenseMatrix.OfColumnMajor(svdParameters.Rows, svdParameters.Rows, compressedData.Take(uSize));
			Matrix S = DiagonalMatrix.OfDiagonal(svdParameters.Rows, svdParameters.Columns, compressedData.Skip(uSize).Take(sSize));
			Matrix VT = DenseMatrix.OfRows(svdParameters.Columns, svdParameters.Columns, compressedData.Skip(uSize + sSize).Partition(svdParameters.Columns));

			// multiply UxSxVT to obtain approximaton of original matrix A
			var US = U.Multiply(S);
			var A_appx = US.Multiply(VT);

			// linearize result to sequence of double arrays
			return A_appx.EnumerateRows().Select(vector => vector.ToArray());
		}

		#endregion

		#region Private methods

		#endregion
	}
}
