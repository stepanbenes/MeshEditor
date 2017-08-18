using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common.Logging;
using MeshEditor.Common.Extensions;

namespace MeshEditor.LayerManager.Compression
{
	internal class SVDCompressionService : ICompressionService
	{
		#region Constructor, Fields

		private readonly bool randomized;
		private readonly double? maxError;
		private readonly double? sizeFactor;

		private readonly ILogger logger;

		public SVDCompressionService(bool randomized, ILogger logger, SVDCompressionFocus focus = SVDCompressionFocus.None, double? factor = null)
		{
			this.randomized = randomized;
			this.logger = logger;
			switch (focus)
			{
				case SVDCompressionFocus.Error:
					maxError = factor;
					break;
				case SVDCompressionFocus.Size:
					sizeFactor = factor;
					break;
			}
		}

		#endregion

		#region Public methods

		public double[] Compress(IEnumerable<double[]> dataValues, int rows, int columns, out CompressionParameters parameters)
		{
			int rank = Math.Min(rows, columns);

			if (rank <= 0)
			{
				parameters = new SVDCompressionParameters
				{
					Rows = rows,
					Columns = columns,
					Rank = rank,
				};

				return new double[0];
			}

			logger?.LogOperationProgress($"Starting SVD compression of matrix {rows}\u00D7{columns}");
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			double[] inputMartix_RowMajor = convertDataValuesToInputMatrixRowMajor(dataValues, rows, columns);

			double[] singularValues, U_VT_columnwise;
			bool resizeIsNeeded = false;

			if (randomized)
			{
				if (sizeFactor.HasValue)
				{
					int newRank = calculateRankFromSizeFactor(rows, columns);
					Debug.Assert(newRank <= rank);
					if (newRank != rank)
					{
						rank = newRank;
						// resize is NOT needed because ComputeSvdRandomized returns singularValues already shrinked to rank length
					}
				}

				// COMPUTE SVD RANDOMIZED
				RedSvdDriver.ComputeSvdRandomized(inputMartix_RowMajor, rows, columns, rank, out singularValues, out U_VT_columnwise);

				Debug.Assert(singularValues.Length == rank);
				Debug.Assert(U_VT_columnwise.Length == rows * rank + rank * columns);
			}
			else
			{
				// COMPUTE SVD EXACT
				RedSvdDriver.ComputeSvdExact(inputMartix_RowMajor, rows, columns, out singularValues, out U_VT_columnwise);

				Debug.Assert(singularValues.Length == rank);
				Debug.Assert(U_VT_columnwise.Length == rows * rank + rank * columns);

				if (sizeFactor.HasValue)
				{
					int newRank = calculateRankFromSizeFactor(rows, columns);
					Debug.Assert(newRank <= rank);
					if (newRank != rank)
					{
						rank = newRank;
						resizeIsNeeded = true;
					}
				}
			}


			if (maxError.HasValue)
			{
				double valueRange = inputMartix_RowMajor.Max() - inputMartix_RowMajor.Min();
				int newRank = calculateRankFromMaxError(singularValues,
								matrixElementCount: rows * columns,
								matrixNorm: (valueRange > 0) ? valueRange : inputMartix_RowMajor.Average() // norm is value range or average value if range is zero
								);
				Debug.Assert(newRank <= rank);
				if (newRank != rank)
				{
					rank = newRank;
					resizeIsNeeded = true;
				}
			}

			stopwatch.Stop();
			logger?.LogOperationProgress($"Singular values calculated (exe-time {stopwatch.Elapsed}). Length: {singularValues.Length}, Input rank: {Math.Min(rows, columns)}, Final rank: {rank}");
#if DEBUG
			logger?.LogOperationProgress(formatSingularValues(singularValues, originalLength: Math.Min(rows, columns), finalLength: rank));
#endif

			if (resizeIsNeeded)
			{
				shrinkSvdOutput(ref singularValues, ref U_VT_columnwise, rows, columns, rank);
				Debug.Assert(singularValues.Length == rank);
				Debug.Assert(U_VT_columnwise.Length == rows * rank + rank * columns);
			}

			double[] US_VT_columnwise = U_VT_columnwise;
			for (int c = 0; c < rank; c++) // multiply U by S
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
				Rank = rank,
			};

			return US_VT_columnwise;
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
		}

		#endregion

		#region Private methods

		private static string formatSingularValues(IReadOnlyList<double> singularValues, int originalLength, int finalLength)
		{
			int currentLength = singularValues.Count;

			Debug.Assert(originalLength >= currentLength);
			Debug.Assert(currentLength >= finalLength);

			List<string> strings = new List<string>();
			for (int i = 0; i < finalLength; i++)
			{
				strings.Add(singularValues[i].ToString());
			}
			for (int i = finalLength; i < currentLength; i++)
			{
				strings.Add("(" + singularValues[i].ToString() + ")");
			}
			for (int i = currentLength; i < originalLength; i++)
			{
				strings.Add("(?)");
			}
			return "singular values: [" + string.Join("; ", strings) + "]";
		}

		private static double[] convertDataValuesToInputMatrixRowMajor(IEnumerable<double[]> dataValues, int rows, int columns)
		{
			double[] inputMatrix_RowMajor = new double[rows * columns];
			using (var enumerator = dataValues.GetEnumerator())
			{
				for (int row = 0; row < rows; row++)
				{
					if (!enumerator.MoveNext())
						throw new ArgumentException("Not enough rows provided for data compression.", nameof(dataValues));
					double[] rowValues = enumerator.Current;
					if (rowValues?.Length != columns)
						throw new ArgumentException("Data row has unexpected length.", nameof(dataValues));
					for (int column = 0; column < columns; column++)
					{
						double value = rowValues[column];
						inputMatrix_RowMajor[row * columns + column] = double.IsNaN(value) ? 0.0 : value; // eliminate NaNs
					}
				}
				if (enumerator.MoveNext())
					throw new ArgumentException("Too many rows provided for data compression.", nameof(dataValues));
			}
			return inputMatrix_RowMajor;
		}

		private static IEnumerable<double[]> calculateDiff(IEnumerable<double[]> firstSequence, IEnumerable<double[]> secondSequence)
		{
			return firstSequence.Zip(secondSequence, (firstRow, secondRow) => firstRow.Zip(secondRow, (firstValue, secondValue) => firstValue - secondValue).ToArray());
		}

		private static void calculateMaxAndAverageError(IEnumerable<double[]> dataValues, IEnumerable<double[]> decompressedValues, out double maxRelativeError, out double averageRelativeError)
		{
			var max = dataValues.Max(row => row.Max());
			var min = dataValues.Min(row => row.Min());
			double range = max - min;
			var allValues = dataValues.Zip(decompressedValues, (firstRow, secondRow) => firstRow.Zip(secondRow, (firstValue, secondValue) => (firstValue - secondValue))).SelectMany(row => row);
			double tempMaxError = double.MinValue;
			double tempErrorSum = 0.0;
			int count = 0;
			foreach (var error in allValues)
			{
				double relativeError = Math.Abs(error) / range;
				tempMaxError = Math.Max(tempMaxError, relativeError);
				tempErrorSum += relativeError;
				count += 1;
			}
			maxRelativeError = tempMaxError;
			averageRelativeError = tempErrorSum / count;
		}

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

		private int calculateRankFromSizeFactor(int rows, int columns)
		{
			Debug.Assert(sizeFactor.HasValue);
			return (int)Math.Ceiling((sizeFactor.Value * rows * columns) / ((double)rows + columns));
		}

		private int calculateRankFromMaxError(IReadOnlyList<double> singularValues, int matrixElementCount, double matrixNorm)
		{
			Debug.Assert(maxError.HasValue);
			Debug.Assert(singularValues.Count > 0);
			Debug.Assert(singularValues.IsOrderedDescending(s => s));
			Debug.Assert(matrixElementCount > 0);
			Debug.Assert(matrixNorm >= 0);
			Debug.Assert(!(matrixNorm > 0 ^ singularValues[0] > 0)); // both must be greater than zero or both must be equal to zero, all at once

			if (double.IsPositiveInfinity(maxError.Value))
				return 0; // allowed error is too high, everything can be thrown away

			// TODO: test cases with zero valueRange and non-zero singular values (same non-zero value for all elements in input matrix)
			//logger.LogMessage("sv: " + string.Join(", ", singularValues));

			double runningMSE = 0.0;
			for (int rank = singularValues.Count - 1; rank >= 0; rank--)
			{
				// Mean square error
				runningMSE += singularValues[rank].Square() / matrixElementCount; // TODO: this equation should be verified
				Debug.Assert(runningMSE == singularValues.Skip(count: rank).Select(s => s.Square()).Sum() / matrixElementCount);

				// Root-mean-square deviation
				double RMSD = Math.Sqrt(runningMSE);

				// Normalized root-mean-square deviation
				double NRMSD = (matrixNorm > 0) ? RMSD / matrixNorm : RMSD; // if valueRange equals to zero then use absolute value instead of relative (RMSD instead of NRMSD)

				//logger.LogMessage($"rank: {rank} MSE: {MSE} RMSD: {RMSD} NRMSD: {NRMSD}");

				if (NRMSD > maxError.Value)
				{
					// if error is higher then prescribed limit then return rank from previous iteration (or full rank for first iteration)
					return rank + 1;
				}
			}

			if (singularValues[0] > 0)
				return 1;

			return 0; // should return 0 only when maxError is Infinite or all singular values are zero
		}

		#endregion
	}
}
