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
			var dataValuesWithoutNaNs = dataValues.Select(row => row.Select(value => double.IsNaN(value) ? 0.0 : value));
			Matrix A = /* SparseMatrix ? */ DenseMatrix.OfRows(rows, columns, dataValuesWithoutNaNs);
			Debug.Assert(A.RowCount == rows);
			Debug.Assert(A.ColumnCount == columns);

			// use MathNet.Numerics' implementation of SVD factorization
			var svd = SVD.Create(A);

			Debug.Assert(svd.U.RowCount == rows);
			Debug.Assert(svd.U.ColumnCount == rows);
			Debug.Assert(svd.S.Count == Math.Min(rows, columns));
			Debug.Assert(svd.VT.RowCount == columns);
			Debug.Assert(svd.VT.ColumnCount == columns);

			int rank;
			if (!decideWhetherToProceedWithCompression(svd.S, rows, columns, out rank))
			{
				// SVD compression is not appropriate,
				// use transparent compression service instead
				var transparentCompression = new TransparentCompressionService();
				return transparentCompression.Compress(dataValues, rows, columns, out parameters); // WARNING: dataValues is enumerated second times !
			}

			parameters = new SVDCompressionParameters
			{
				Rows = rows,
				Columns = columns,
				Rank = rank,
			};

			if (rank == 0) // if rank is zero, matrix A is full of zeroes, so it enables ultimate compression
			{
				return new double[0];
			}

			int u_rows = rows;
			int u_columns = rank;
			int vt_rows = rank;
			int vt_columns = columns;

			// linearize vectors in matrice U, V, S to double array
			double[] result = new double[u_rows * u_columns + rank + vt_rows * vt_columns];

			double[] uColumnWise = svd.U.EnumerateColumns(0, u_columns).SelectMany(column => column).ToArray(); // take newRank columns of U
			Debug.Assert(uColumnWise.Length == u_rows * u_columns);
			Array.Copy(uColumnWise, result, uColumnWise.Length);
			int offset = uColumnWise.Length;
			//uColumnWise = null;

			double[] sDiagonal = svd.S.Take(rank).ToArray(); // take newRank singular values
			Debug.Assert(sDiagonal.Length == rank);
			Array.Copy(sDiagonal, 0, result, offset, sDiagonal.Length);
			offset += sDiagonal.Length;
			//sDiagonal = null;

			double[] vtColumnWise = svd.VT.EnumerateRows(0, vt_rows).SelectMany(row => row).ToArray(); // take newRank rows of VT
			Debug.Assert(vtColumnWise.Length == vt_rows * vt_columns);
			Array.Copy(vtColumnWise, 0, result, offset, vtColumnWise.Length);
			offset += vtColumnWise.Length;
			//vtColumnWise = null;
			Debug.Assert(offset == result.Length);

#if DEBUG
			// evaluate compression quality and save the results to parameters object

			var decompressedData = Decompress(result, parameters);
			double globalMin = double.MaxValue, globalMax = double.MinValue, maxError = double.MinValue;

			using (var decompressedDataEnumerator = decompressedData.GetEnumerator())
			{
				for (int row = 0; row < rows; row++)
				{
					if (!decompressedDataEnumerator.MoveNext())
					{
						throw new InvalidOperationException();
					}
					double[] decompressedRow = decompressedDataEnumerator.Current;
					for (int column = 0; column < columns; column++)
					{
						double originalValue = A[row, column];
						double decompressedValue = decompressedRow[column];
						globalMin = Math.Min(globalMin, originalValue);
						globalMax = Math.Max(globalMax, originalValue);
						double error = Math.Abs(originalValue - decompressedValue);
						maxError = Math.Max(maxError, error);
					}
				}
			}

			double range = globalMax - globalMin;
			double maxRelativeError = maxError / range;
			var svdParameters = (SVDCompressionParameters)parameters;
			svdParameters.MaxDataValue = globalMax;
			svdParameters.MinDataValue = globalMin;
			svdParameters.MaxRelativeError = maxRelativeError;
			svdParameters.CompressionFactor = computeCompressionFactor(rank, rows, columns);
#endif

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
			int u_size = svdParameters.Rows * svdParameters.Rank;
			int s_size = svdParameters.Rank;
			int vt_size = svdParameters.Rank * svdParameters.Columns;
			Debug.Assert(u_size + s_size + vt_size == compressedData.Length);

			// TODO: get rid of Concat methods, simulate multiplication of complete matrices more efficiently
			Matrix U = DenseMatrix.OfColumnMajor(svdParameters.Rows, svdParameters.Rows, compressedData.Take(u_size).Concat(Enumerable.Repeat(0.0, svdParameters.Rows * svdParameters.Rows - u_size)));
			Matrix S = DiagonalMatrix.OfDiagonal(svdParameters.Rows, svdParameters.Columns, compressedData.Skip(u_size).Take(s_size).Concat(Enumerable.Repeat(0.0, Math.Min(svdParameters.Rows, svdParameters.Columns) - s_size)));
			Matrix VT = DenseMatrix.OfRows(svdParameters.Columns, svdParameters.Columns, compressedData.Skip(u_size + s_size).Partition(svdParameters.Columns).Concat(Enumerable.Repeat(new double[svdParameters.Columns], svdParameters.Columns - svdParameters.Rank)));

			// multiply UxSxVT to obtain approximaton of original matrix A
			var US = U.Multiply(S);
			var A_appx = US.Multiply(VT);

			// linearize result to sequence of double arrays
			return A_appx.EnumerateRows().Select(vector => vector.ToArray());
		}

		#endregion

		#region Private methods

		private static bool decideWhetherToProceedWithCompression(IList<double> singularValues, int inputMatrixRowCount, int inputMatrixColumnCount, out int rank)
		{
			// TODO: are singularValues really sorted?

			//double tolerance = MathNet.Numerics.Precision.EpsilonOf(singularValues.Max()) * Math.Max(inputMatrixRowCount, inputMatrixColumnCount);
			double tolerance = singularValues.Max() * 1e-3; // TODO: add smart calculation of tolerance
			rank = singularValues.Count(t => Math.Abs(t) > tolerance);
			double factor = computeCompressionFactor(rank, inputMatrixRowCount, inputMatrixColumnCount);
			return factor < 1.0;
		}

		private static double computeCompressionFactor(int rank, int inputMatrixRowCount, int inputMatrixColumnCount)
		{
			return rank * ((double)inputMatrixRowCount + inputMatrixColumnCount + 1) / ((double)inputMatrixRowCount * inputMatrixColumnCount);
		}

		#endregion
	}
}
