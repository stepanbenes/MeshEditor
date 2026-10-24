using System;
using System.Linq;
using MeshEditor.LayerManager.Compression;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Managed SVD used by the SVD compression of imported results (it replaced the native redsvd library, issue #11).
	/// </summary>
	public class SvdTests
	{
		private static double[] randomMatrix(int rows, int columns, int seed)
		{
			var random = new Random(seed);
			return Enumerable.Range(0, rows * columns).Select(_ => random.NextDouble() * 200.0 - 100.0).ToArray();
		}

		/// <summary>Matrix of the given rank: sum of <paramref name="rank"/> outer products.</summary>
		private static double[] lowRankMatrix(int rows, int columns, int rank, int seed)
		{
			double[] a = randomMatrix(rows, rank, seed), b = randomMatrix(rank, columns, seed + 1);
			var matrix = new double[rows * columns];
			for (int r = 0; r < rows; r++)
				for (int c = 0; c < columns; c++)
					for (int k = 0; k < rank; k++)
						matrix[r * columns + c] += a[r * rank + k] * b[k * columns + c];
			return matrix;
		}

		/// <summary>U S V^T in row-major order; U and V^T are column-major in <paramref name="U_VT"/>.</summary>
		private static double[] reconstruct(double[] singularValues, double[] U_VT, int rows, int columns, int rank)
		{
			var matrix = new double[rows * columns];
			for (int r = 0; r < rows; r++)
				for (int c = 0; c < columns; c++)
					for (int k = 0; k < rank; k++)
						matrix[r * columns + c] += U_VT[k * rows + r] * singularValues[k] * U_VT[rows * rank + c * rank + k];
			return matrix;
		}

		private static double maxDifference(double[] a, double[] b) => a.Zip(b, (x, y) => Math.Abs(x - y)).Max();

		[TestCase(50, 8)]
		[TestCase(8, 50)]
		[TestCase(13, 13)]
		[TestCase(1, 7)]
		[TestCase(7, 1)]
		public void Exact_SVD_reconstructs_the_matrix(int rows, int columns)
		{
			double[] matrix = randomMatrix(rows, columns, seed: rows * 100 + columns);
			RedSvdDriver.ComputeSvdExact(matrix, rows, columns, out double[] singularValues, out double[] U_VT);

			int rank = Math.Min(rows, columns);
			Assert.That(singularValues, Has.Length.EqualTo(rank));
			Assert.That(U_VT, Has.Length.EqualTo(rows * rank + rank * columns));
			Assert.That(singularValues, Is.Ordered.Descending);
			Assert.That(singularValues, Is.All.GreaterThanOrEqualTo(0.0));
			Assert.That(maxDifference(reconstruct(singularValues, U_VT, rows, columns, rank), matrix), Is.LessThan(1e-9));
		}

		[Test]
		public void Exact_SVD_of_zero_matrix_has_zero_singular_values()
		{
			RedSvdDriver.ComputeSvdExact(new double[6 * 4], 6, 4, out double[] singularValues, out double[] U_VT);
			Assert.That(singularValues, Is.All.EqualTo(0.0));
			Assert.That(U_VT, Has.Length.EqualTo(6 * 4 + 4 * 4));
			Assert.That(U_VT, Is.All.Not.NaN);
		}

		[TestCase(200, 20, 3)]
		[TestCase(20, 200, 3)]
		public void Randomized_SVD_reconstructs_a_low_rank_matrix(int rows, int columns, int matrixRank)
		{
			double[] matrix = lowRankMatrix(rows, columns, matrixRank, seed: 7);
			RedSvdDriver.ComputeSvdExact(matrix, rows, columns, out double[] exactSingularValues, out _);

			RedSvdDriver.ComputeSvdRandomized(matrix, rows, columns, matrixRank, out double[] singularValues, out double[] U_VT);

			Assert.That(singularValues, Has.Length.EqualTo(matrixRank));
			Assert.That(U_VT, Has.Length.EqualTo(rows * matrixRank + matrixRank * columns));
			Assert.That(singularValues, Is.EqualTo(exactSingularValues.Take(matrixRank)).Within(1e-6).Percent);
			double scale = matrix.Max(Math.Abs);
			Assert.That(maxDifference(reconstruct(singularValues, U_VT, rows, columns, matrixRank), matrix), Is.LessThan(1e-9 * scale));
		}

		[Test]
		public void Randomized_SVD_with_rank_above_the_matrix_rank_has_no_NaNs()
		{
			// Gram-Schmidt zeroes the dependent columns
			double[] matrix = lowRankMatrix(30, 10, 2, seed: 3);
			RedSvdDriver.ComputeSvdRandomized(matrix, 30, 10, 6, out double[] singularValues, out double[] U_VT);

			Assert.That(singularValues, Is.All.Not.NaN);
			Assert.That(U_VT, Is.All.Not.NaN);
			Assert.That(maxDifference(reconstruct(singularValues, U_VT, 30, 10, 6), matrix), Is.LessThan(1e-9 * matrix.Max(Math.Abs)));
		}
	}
}
