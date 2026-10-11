using System;
using System.Diagnostics;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace MeshEditor.LayerManager.Compression
{
	/// <summary>
	/// Managed replacement of the former native redsvd library (Eigen JacobiSVD + RedSVD by Daisuke Okanohara).
	/// Output layout: singular values in descending order; U (rows × rank) column-major followed by V^T (rank × columns) column-major.
	/// </summary>
	public static class RedSvdDriver
	{
		private const double GramSchmidtEpsilon = 0.000001;
		private const int RandomSeed = 1; // deterministic output, like the native rand() that was never seeded

		public static void ComputeSvdExact(double[] inputMatrix_RowMajor, int numberOfRows, int numberOfColumns, out double[] singularValues, out double[] U_VT_ColumnMajor)
		{
			Debug.Assert(inputMatrix_RowMajor != null);
			Debug.Assert(inputMatrix_RowMajor.Length == numberOfRows * numberOfColumns);

			Matrix<double> A = Matrix<double>.Build.DenseOfRowMajor(numberOfRows, numberOfColumns, inputMatrix_RowMajor);
			Matrix<double> U, V;
			Vector<double> S;
			if (numberOfRows >= numberOfColumns)
			{
				computeThinSvdOfTallMatrix(A, out U, out S, out V);
			}
			else
			{
				computeThinSvdOfTallMatrix(A.Transpose(), out V, out S, out U); // A^T = V S U^T
			}

			assembleOutput(U, S, V, out singularValues, out U_VT_ColumnMajor);

			int rank = Math.Min(numberOfRows, numberOfColumns);
			Debug.Assert(singularValues.Length == rank);
			Debug.Assert(U_VT_ColumnMajor.Length == numberOfRows * rank + rank * numberOfColumns);
		}

		public static void ComputeSvdRandomized(double[] inputMatrix_RowMajor, int numberOfRows, int numberOfColumns, int rank, out double[] singularValues, out double[] U_VT_ColumnMajor)
		{
			Debug.Assert(inputMatrix_RowMajor != null);
			Debug.Assert(inputMatrix_RowMajor.Length == numberOfRows * numberOfColumns);
			Debug.Assert(rank > 0 && rank <= Math.Min(numberOfRows, numberOfColumns));

			Matrix<double> A = Matrix<double>.Build.DenseOfRowMajor(numberOfRows, numberOfColumns, inputMatrix_RowMajor);
			var normal = new Normal(0.0, 1.0, new Random(RandomSeed));

			// Gaussian random matrix for A^T
			Matrix<double> O = Matrix<double>.Build.Random(numberOfRows, rank, normal);
			// Sample matrix of A^T
			Matrix<double> Y = A.TransposeThisAndMultiply(O);
			orthonormalizeColumns(Y);
			// Range(B) = Range(A^T)
			Matrix<double> B = A * Y;
			// Gaussian random matrix
			Matrix<double> P = Matrix<double>.Build.Random(rank, rank, normal);
			// Sample matrix of B
			Matrix<double> Z = B * P;
			orthonormalizeColumns(Z);
			// Range(C) = Range(B)
			Matrix<double> C = Z.TransposeThisAndMultiply(B);

			// C = U S V^T  =>  A = Z U S V^T Y^T
			Svd<double> svdOfC = C.Svd(computeVectors: true);
			assembleOutput(Z * svdOfC.U, svdOfC.S, Y * svdOfC.VT.Transpose(), out singularValues, out U_VT_ColumnMajor);

			Debug.Assert(singularValues.Length == rank);
			Debug.Assert(U_VT_ColumnMajor.Length == numberOfRows * rank + rank * numberOfColumns);
		}

		/// <summary>A = Q R (thin), R = U_R S V^T  =>  A = (Q U_R) S V^T. Keeps U at rows × columns instead of the full rows × rows.</summary>
		private static void computeThinSvdOfTallMatrix(Matrix<double> A, out Matrix<double> U, out Vector<double> S, out Matrix<double> V)
		{
			Debug.Assert(A.RowCount >= A.ColumnCount);
			QR<double> qr = A.QR(QRMethod.Thin);
			Svd<double> svdOfR = qr.R.Svd(computeVectors: true);
			U = qr.Q * svdOfR.U;
			S = svdOfR.S;
			V = svdOfR.VT.Transpose();
		}

		/// <summary>Modified Gram-Schmidt; once a column becomes (numerically) dependent, it and all following columns are zeroed.</summary>
		private static void orthonormalizeColumns(Matrix<double> matrix)
		{
			for (int i = 0; i < matrix.ColumnCount; i++)
			{
				Vector<double> column = matrix.Column(i);
				for (int j = 0; j < i; j++)
				{
					Vector<double> previousColumn = matrix.Column(j);
					column -= column.DotProduct(previousColumn) * previousColumn;
				}
				double norm = column.L2Norm();
				if (norm < GramSchmidtEpsilon)
				{
					matrix.ClearColumns(Enumerable.Range(i, matrix.ColumnCount - i).ToArray());
					return;
				}
				matrix.SetColumn(i, column / norm);
			}
		}

		private static void assembleOutput(Matrix<double> U, Vector<double> S, Matrix<double> V, out double[] singularValues, out double[] U_VT_ColumnMajor)
		{
			singularValues = S.ToArray();

			double[] u = U.ToColumnMajorArray();
			double[] vt = V.ToRowMajorArray(); // V row-major = V^T column-major
			U_VT_ColumnMajor = new double[u.Length + vt.Length];
			u.CopyTo(U_VT_ColumnMajor, 0);
			vt.CopyTo(U_VT_ColumnMajor, u.Length);
		}
	}
}
