using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Common;

namespace MeshEditor.LayerManager.Compression
{
	internal class SVDCompressionService : ICompressionService
	{
		#region Constructor, Fields

		private readonly double? singularValueToleranceFactor;
		private readonly double? desiredCompressionFactor;

		public SVDCompressionService(SVDCompressionStrategy strategy = SVDCompressionStrategy.None, double factor = 1.0)
		{
			switch (strategy)
			{
				case SVDCompressionStrategy.Quality:
					singularValueToleranceFactor = factor;
					break;
				case SVDCompressionStrategy.Size:
					desiredCompressionFactor = factor;
					break;
			}
		}

		#endregion

		#region Public methods

		public double[] Compress(IEnumerable<double[]> dataValues, int rows, int columns, out CompressionParameters parameters)
		{
			int rank = calculateRank(rows, columns);

			Debug.Assert(rank >= 0 && rank <= Math.Min(rows, columns));

			if (rank <= 0)
			{
				parameters = new SVDCompressionParameters
				{
					Rows = rows,
					Columns = columns,
					Rank = 0,
				};

				return new double[0];
			}

			REDSVD.Driver driver = new REDSVD.Driver();
			double[] singularValues, U_VT_columnwise;
			driver.Run(dataValues, rows, columns, rank, out singularValues, out U_VT_columnwise);

			Debug.Assert(singularValues.Length == rank);
			Debug.Assert(U_VT_columnwise.Length == rows * rank + rank * columns);

			int updatedRank = calculateRank(singularValues);

			if (updatedRank != rank)
			{
				Debug.Assert(updatedRank < rank);
				shrinkSvdOutput(ref singularValues, ref U_VT_columnwise, rows, columns, updatedRank);
				Debug.Assert(singularValues.Length == updatedRank);
				Debug.Assert(U_VT_columnwise.Length == rows * updatedRank + updatedRank * columns);
			}

			double[] US_VT_columnwise = U_VT_columnwise;
			for (int c = 0; c < updatedRank; c++)
			{
				double singularValue = singularValues[c];
				for (int r = 0; r < rows; r++)
				{
					US_VT_columnwise[c * rows + r] *= singularValue;
				}
			}

			parameters = new SVDCompressionParameters
			{
				Rows = rows,
				Columns = columns,
				Rank = updatedRank,
			};

			return US_VT_columnwise;

			//// create matrix from input data values, replace possible NaN values with zeroes
			//var dataValuesWithoutNaNs = dataValues.Select(row => row.Select(value => double.IsNaN(value) ? 0.0 : value));
			//Matrix A = /* SparseMatrix ? */ DenseMatrix.OfRows(rows, columns, dataValuesWithoutNaNs);
			//Debug.Assert(A.RowCount == rows);
			//Debug.Assert(A.ColumnCount == columns);

			//// use MathNet.Numerics' implementation of SVD factorization
			//var svd = SVD.Create(A);

			//Debug.Assert(svd.U.RowCount == rows);
			//Debug.Assert(svd.U.ColumnCount == rows);
			//Debug.Assert(svd.S.Count == Math.Min(rows, columns));
			//Debug.Assert(svd.VT.RowCount == columns);
			//Debug.Assert(svd.VT.ColumnCount == columns);

			//int rank;
			//if (!decideWhetherToProceedWithCompression(svd.S, rows, columns, out rank))
			//{
			//	// SVD compression is not appropriate,
			//	// use transparent compression service instead
			//	var transparentCompression = new TransparentCompressionService();
			//	return transparentCompression.Compress(dataValues, rows, columns, out parameters); // WARNING: dataValues is enumerated second times !
			//}

			//parameters = new SVDCompressionParameters
			//{
			//	Rows = rows,
			//	Columns = columns,
			//	Rank = rank,
			//};

			//if (rank == 0) // if rank is zero, matrix A is full of zeroes, so it enables ultimate compression
			//{
			//	return new double[0];
			//}

			//int u_rows = rows;
			//int u_columns = rank;
			//int vt_rows = rank;
			//int vt_columns = columns;

			//// linearize vectors in matrice U, V, S to double array
			//double[] result = new double[u_rows * u_columns + rank + vt_rows * vt_columns];

			//double[] uColumnWise = svd.U.EnumerateColumns(0, u_columns).SelectMany(column => column).ToArray(); // take newRank columns of U
			//Debug.Assert(uColumnWise.Length == u_rows * u_columns);
			//Array.Copy(uColumnWise, result, uColumnWise.Length);
			//int offset = uColumnWise.Length;
			////uColumnWise = null;

			//double[] sDiagonal = svd.S.Take(rank).ToArray(); // take newRank singular values
			//Debug.Assert(sDiagonal.Length == rank);
			//Array.Copy(sDiagonal, 0, result, offset, sDiagonal.Length);
			//offset += sDiagonal.Length;
			////sDiagonal = null;

			//double[] vtColumnWise = svd.VT.EnumerateRows(0, vt_rows).SelectMany(row => row).ToArray(); // take newRank rows of VT
			//Debug.Assert(vtColumnWise.Length == vt_rows * vt_columns);
			//Array.Copy(vtColumnWise, 0, result, offset, vtColumnWise.Length);
			//offset += vtColumnWise.Length;
			////vtColumnWise = null;
			//Debug.Assert(offset == result.Length);

//#if DEBUG
//			// evaluate compression quality and save the results to parameters object

//			var decompressedData = Decompress(result, parameters);
//			double globalMin = double.MaxValue, globalMax = double.MinValue, maxError = double.MinValue;

//			using (var decompressedDataEnumerator = decompressedData.GetEnumerator())
//			{
//				for (int row = 0; row < rows; row++)
//				{
//					if (!decompressedDataEnumerator.MoveNext())
//					{
//						throw new InvalidOperationException();
//					}
//					double[] decompressedRow = decompressedDataEnumerator.Current;
//					for (int column = 0; column < columns; column++)
//					{
//						double originalValue = A[row, column];
//						double decompressedValue = decompressedRow[column];
//						globalMin = Math.Min(globalMin, originalValue);
//						globalMax = Math.Max(globalMax, originalValue);
//						double error = Math.Abs(originalValue - decompressedValue);
//						maxError = Math.Max(maxError, error);
//					}
//				}
//			}

//			double range = globalMax - globalMin;
//			double maxRelativeError = maxError / range;
//			var svdParameters = (SVDCompressionParameters)parameters;
//			svdParameters.MaxDataValue = globalMax;
//			svdParameters.MinDataValue = globalMin;
//			svdParameters.MaxRelativeError = maxRelativeError;
//			svdParameters.CompressionFactor = computeCompressionFactor(rank, rows, columns);
//#endif
//
//			return result;
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

			return multiplyUSandVTandEnumerateRowsOfResultMatrix(compressedData, svdParameters.Rows, svdParameters.Columns, svdParameters.Rank);

			// create matrices U, S, VT from compressedData
			//int u_size = svdParameters.Rows * svdParameters.Rank;
			//int s_size = svdParameters.Rank;
			//int v_size = svdParameters.Columns * svdParameters.Rank;
			//Debug.Assert(u_size + s_size + v_size == compressedData.Length);

			//// TODO: get rid of Concat methods, simulate multiplication of complete matrices more efficiently
			//Matrix U = DenseMatrix.OfColumnMajor(svdParameters.Rows, svdParameters.Rows, compressedData.Take(u_size).Concat(Enumerable.Repeat(0.0, svdParameters.Rows * svdParameters.Rows - u_size)));
			//Matrix S = DiagonalMatrix.OfDiagonal(svdParameters.Rows, svdParameters.Columns, compressedData.Skip(u_size).Take(s_size).Concat(Enumerable.Repeat(0.0, Math.Min(svdParameters.Rows, svdParameters.Columns) - s_size)));
			//Matrix VT = DenseMatrix.OfRows(svdParameters.Columns, svdParameters.Columns, compressedData.Skip(u_size + s_size).Partition(svdParameters.Columns).Concat(Enumerable.Repeat(new double[svdParameters.Columns], svdParameters.Columns - svdParameters.Rank)));

			//// multiply UxSxVT to obtain approximaton of original matrix A
			//var US = U.Multiply(S);
			//var A_appx = US.TransposeAndMultiply(VT);

			//// linearize result to sequence of double arrays
			//return A_appx.EnumerateRows().Select(vector => vector.ToArray());
		}

		#endregion

		#region Private methods

		private static void shrinkSvdOutput(ref double[] singularValues, ref double[] U_VT_columnwise, int rows, int columns, int newRank)
		{
			int oldRank = singularValues.Length;
			Debug.Assert(newRank < oldRank);

			Array.Resize(ref singularValues, newRank);

			int U_oldLength = rows * oldRank;
			int U_newLength = rows * newRank;
			int V_newLength = newRank * columns;

			double[] shrinked_U_VT_columnwise = new double[U_newLength + V_newLength];

			Array.Copy(U_VT_columnwise, 0, shrinked_U_VT_columnwise, 0, U_newLength);

			for (int i = 0; i < columns; i++)
			{
				int oldOffset = U_oldLength + i * oldRank;
				int newOffset = U_newLength + i * newRank;
				Array.Copy(U_VT_columnwise, oldOffset, shrinked_U_VT_columnwise, newOffset, newRank);
			}

			U_VT_columnwise = shrinked_U_VT_columnwise;
		}

		private static IEnumerable<double[]> multiplyUSandVTandEnumerateRowsOfResultMatrix(double[] US_VT_columnwise, int rows, int columns, int rank)
		{
			Debug.Assert(US_VT_columnwise.Length == rows * rank + rank * columns);

			int U_length = rows * rank;

			for (int i = 0; i < rows; i++)
			{
				double[] A_approx_row = new double[columns];
				for (int j = 0; j < columns; j++)
				{
					for (int k = 0; k < rank; k++)
					{
						double US_value = US_VT_columnwise[k * rows + i];
						double VT_value = US_VT_columnwise[U_length + j * rank + k];
						A_approx_row[j] += US_value * VT_value;
					}
				}
				yield return A_approx_row;
			}
		}

		private int calculateRank(int rows, int columns)
		{
			if (desiredCompressionFactor.HasValue)
			{
				return (int)Math.Ceiling((desiredCompressionFactor.Value * rows * columns)/((double)rows + columns));
			}
			return Math.Min(rows, columns);
		}

		private int calculateRank(IReadOnlyList<double> singularValues)
		{
			Debug.Assert(singularValues != null);
			if (singularValues.Count == 0)
				return 0;
			if (singularValueToleranceFactor.HasValue)
			{
				// NOTE: I assume that singular values are sorted in descending order
				Debug.Assert(singularValues.IsOrderedDescending(sv => sv));
				double firstSingularValue = singularValues[0];
				double tolerance = (1.0 - singularValueToleranceFactor.Value) * Math.Abs(firstSingularValue);
				return singularValues.TakeWhile(sv => Math.Abs(sv) > tolerance).Count();
			}
			return singularValues.Count;
		}

		#endregion
	}
}
