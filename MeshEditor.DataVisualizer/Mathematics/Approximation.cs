using MeshEditor.CoreInterface;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Mathematics
{
	public static class Approximation
	{
		public static int GetMinNumberOfDataPoints(ApproximationMethod method)
		{
			switch (method)
			{
				case ApproximationMethod.ConstantValue:
					return 1;
				case ApproximationMethod.LinearRegression:
					return 4;
				case ApproximationMethod.TrilinearRegression:
					return 8;
				case ApproximationMethod.TrilinearInterpolation:
					return 1;
				case ApproximationMethod.QuadraticRegression:
					return 10;
				default:
					throw new NotSupportedException();
			}
		}

		public static Polynomial DoApproximation(IList<float> x, IList<float> y, IList<float> z, IList<float> w, ApproximationMethod method = ApproximationMethod.Default)
		{
			int number = x.Count;
			Debug.Assert(number > 0);

			if (method == ApproximationMethod.Default)
			{
				if (number >= GetMinNumberOfDataPoints(ApproximationMethod.TrilinearRegression))
					method = ApproximationMethod.TrilinearRegression;
				else if (number >= GetMinNumberOfDataPoints(ApproximationMethod.LinearRegression))
					method = ApproximationMethod.LinearRegression;
				else
					method = ApproximationMethod.ConstantValue;
			}

			switch (method)
			{
				case ApproximationMethod.LinearRegression:
					return DoLinearRegression(x, y, z, w);
				case ApproximationMethod.TrilinearRegression:
					return DoLSTI(x, y, z, w);
				case ApproximationMethod.TrilinearInterpolation:
					return DoTrilinearIrregularInterpolation(x, y, z, w);
				case ApproximationMethod.QuadraticRegression:
					return DoQuadraticRegression(x, y, z, w);
				case ApproximationMethod.ConstantValue:
					return new ConstantValue(w.Average());
				default:
					throw new NotSupportedException();
			}
		}

		public static HyperPlane DoLinearRegression(IList<float> x, IList<float> y, IList<float> z, IList<float> w)
		{
			int n = x.Count;
			Debug.Assert(x.Count == n && y.Count == n && z.Count == n && w.Count == n);
			Debug.Assert(n >= GetMinNumberOfDataPoints(ApproximationMethod.LinearRegression)); /**/

			Matrix A = new Matrix(4, 4);
			Matrix b = new Matrix(4, 1);

			for (int i = 0; i < n; i++)
			{
				A[0, 0] += x[i] * x[i];
				A[0, 1] += x[i] * y[i];
				A[0, 2] += x[i] * z[i];
				A[0, 3] += x[i];

				A[1, 1] += y[i] * y[i];
				A[1, 2] += y[i] * z[i];
				A[1, 3] += y[i];

				A[2, 2] += z[i] * z[i];
				A[2, 3] += z[i];

				b[0, 0] += x[i] * w[i];
				b[1, 0] += y[i] * w[i];
				b[2, 0] += z[i] * w[i];
				b[3, 0] += w[i];
			}

			A[3, 3] = n;

			for (int i = 0; i < 4; i++) // fill symetric cells
			{
				for (int j = i + 1; j < 4; j++)
				{
					A[j, i] = A[i, j];
				}
			}

			return new HyperPlane(solve(A, b));
		}

		public static HyperQuadric DoQuadraticRegression(IList<float> x, IList<float> y, IList<float> z, IList<float> w)
		{
			int n = x.Count;
			Debug.Assert(x.Count == n && y.Count == n && z.Count == n && w.Count == n);
			Debug.Assert(n >= GetMinNumberOfDataPoints(ApproximationMethod.QuadraticRegression)); /**/

			// G(a..j) = (w - ax^2 - by^2 - cz^2 - dxy - exz - fyz - gx - hy - iz - j) ^ 2

			Matrix A = new Matrix(10, 10);
			Matrix b = new Matrix(10, 1);

			for (int i = 0; i < n; i++)
			{
				// dG/da = 0
				A[0, 0] += BIQ(x[i]);								// a
				A[0, 1] += SQR(x[i]) * SQR(y[i]);					// b
				A[0, 2] += SQR(x[i]) * SQR(z[i]);					// c
				A[0, 3] += CUB(x[i]) * y[i];						// d
				A[0, 4] += CUB(x[i]) * z[i];						// e
				A[0, 5] += SQR(x[i]) * y[i] * z[i];					// f
				A[0, 6] += CUB(x[i]);								// g
				A[0, 7] += SQR(x[i]) * y[i];						// h
				A[0, 8] += SQR(x[i]) * z[i];						// i
				A[0, 9] += SQR(x[i]);								// j

				// dG/db = 0
				A[1, 1] += BIQ(y[i]);								// b
				A[1, 2] += SQR(y[i]) * SQR(z[i]);					// c
				A[1, 3] += x[i] * CUB(y[i]);						// d
				A[1, 4] += x[i] * SQR(y[i]) * z[i];					// e
				A[1, 5] += CUB(y[i]) * z[i];						// f
				A[1, 6] += x[i] * SQR(y[i]);						// g
				A[1, 7] += CUB(y[i]);								// h
				A[1, 8] += SQR(y[i]) * z[i];						// i
				A[1, 9] += SQR(y[i]);								// j

				// dG/dc = 0
				A[2, 2] += BIQ(z[i]);								// c
				A[2, 3] += x[i] * y[i] * SQR(z[i]);					// d
				A[2, 4] += x[i] * CUB(z[i]);						// e
				A[2, 5] += y[i] * CUB(z[i]);						// f
				A[2, 6] += x[i] * SQR(z[i]);						// g
				A[2, 7] += y[i] * SQR(z[i]);						// h
				A[2, 8] += CUB(z[i]);								// i
				A[2, 9] += SQR(z[i]);								// j

				// dG/dd = 0
				A[3, 3] += SQR(x[i]) * SQR(y[i]);					// d
				A[3, 4] += SQR(x[i]) * y[i] * z[i];					// e
				A[3, 5] += x[i] * SQR(y[i]) * z[i];					// f
				A[3, 6] += SQR(x[i]) * y[i];						// g
				A[3, 7] += x[i] * SQR(y[i]);						// h
				A[3, 8] += x[i] * y[i] * z[i];						// i
				A[3, 9] += x[i] * y[i];								// j

				// dG/de = 0
				A[4, 4] += SQR(x[i]) * SQR(z[i]);					// e
				A[4, 5] += x[i] * y[i] * SQR(z[i]);					// f
				A[4, 6] += SQR(x[i]) * z[i];						// g
				A[4, 7] += x[i] * y[i] * z[i];						// h
				A[4, 8] += x[i] * SQR(z[i]);						// i
				A[4, 9] += x[i] * z[i];								// j

				// dG/df = 0
				A[5, 5] += SQR(y[i]) * SQR(z[i]);					// f
				A[5, 6] += x[i] * y[i] * z[i];						// g
				A[5, 7] += SQR(y[i]) * z[i];						// h
				A[5, 8] += y[i] * SQR(z[i]);						// i
				A[5, 9] += y[i] * z[i];								// j

				// dG/dg = 0
				A[6, 6] += SQR(x[i]);								// g
				A[6, 7] += x[i] * y[i];								// h
				A[6, 8] += x[i] * z[i];								// i
				A[6, 9] += x[i];									// j

				// dG/dh = 0
				A[7, 7] += SQR(y[i]);								// h
				A[7, 8] += y[i] * z[i];								// i
				A[7, 9] += y[i];									// j

				// dG/di = 0
				A[8, 8] += SQR(z[i]);								// i
				A[8, 9] += z[i];									// j

				// right column
				b[0, 0] += w[i] * SQR(x[i]);						// a
				b[1, 0] += w[i] * SQR(y[i]);						// b
				b[2, 0] += w[i] * SQR(z[i]);						// c
				b[3, 0] += w[i] * x[i] * y[i];						// d
				b[4, 0] += w[i] * x[i] * z[i];						// e
				b[5, 0] += w[i] * y[i] * z[i];						// f
				b[6, 0] += w[i] * x[i];								// g
				b[7, 0] += w[i] * y[i];								// h
				b[8, 0] += w[i] * z[i];								// i
				b[9, 0] += w[i];									// j
			}

			// dG/dj = 0
			A[9, 9] = n;											// j

			for (int i = 0; i < 10; i++) // fill symetric cells
			{
				for (int j = i + 1; j < 10; j++)
				{
					A[j, i] = A[i, j];
				}
			}

			return new HyperQuadric(solve(A, b));
		}

		/// <summary>
		/// Least Squares Trilinear Interpolation method.
		/// </summary>
		/// <returns>Parameters of trilinear algebraic expression.</returns>
		public static TrilinearForm DoLSTI(IList<float> x, IList<float> y, IList<float> z, IList<float> w)
		{
			int n = x.Count;
			Debug.Assert(x.Count == n && y.Count == n && z.Count == n && w.Count == n);
			Debug.Assert(n >= GetMinNumberOfDataPoints(ApproximationMethod.TrilinearRegression)); /**/

			Matrix A = new Matrix(8, 8);
			Matrix b = new Matrix(8, 1);

			// fill matrix
			for (int i = 0; i < n; i++)
			{
				// dG/da
				A[0, 0] += SQR(x[i]) * SQR(y[i]) * SQR(z[i]);
				A[0, 1] += SQR(x[i]) * SQR(y[i]) * z[i];
				A[0, 2] += SQR(x[i]) * y[i] * SQR(z[i]);
				A[0, 3] += x[i] * SQR(y[i]) * SQR(z[i]);
				A[0, 4] += SQR(x[i]) * y[i] * z[i];
				A[0, 5] += x[i] * SQR(y[i]) * z[i];
				A[0, 6] += x[i] * y[i] * SQR(z[i]);
				A[0, 7] += x[i] * y[i] * z[i];
				
				b[0, 0] += w[i] * x[i] * y[i] * z[i];
				
				// dG/db
				A[1, 1] += SQR(x[i]) * SQR(y[i]);
				//A[1, 2] += SQR(x[i]) * y[i] * z[i];
				//A[1, 3] += x[i] * SQR(y[i]) * z[i];
				A[1, 4] += SQR(x[i]) * y[i];
				A[1, 5] += x[i] * SQR(y[i]);
				//A[1, 6] += x[i] * y[i] * z[i];
				A[1, 7] += x[i] * y[i];
				
				b[1, 0] += w[i] * x[i] * y[i];

				// dG/dc
				A[2, 2] += SQR(x[i]) * SQR(z[i]);
				//A[2, 3] += x[i] * y[i] * SQR(z[i]);
				A[2, 4] += SQR(x[i]) * z[i];
				//A[2, 5] += x[i] * y[i] * z[i];
				A[2, 6] += x[i] * SQR(z[i]);
				A[2, 7] += x[i] * z[i];

				b[2, 0] += w[i] * x[i] * z[i];

				// dG/dd
				A[3, 3] += SQR(y[i]) * SQR(z[i]);
				//A[3, 4] += x[i] * y[i] * z[i];
				A[3, 5] += SQR(y[i]) * z[i];
				A[3, 6] += y[i] * SQR(z[i]);
				A[3, 7] += y[i] * z[i];

				b[3, 0] += w[i] * y[i] * z[i];

				// dG/de
				A[4, 4] += SQR(x[i]);
				A[4, 5] += x[i] * y[i];
				A[4, 6] += x[i] * z[i];
				A[4, 7] += x[i];

				b[4, 0] += w[i] * x[i];

				// dG/df
				A[5, 5] += SQR(y[i]);
				A[5, 6] += y[i] * z[i];
				A[5, 7] += y[i];

				b[5, 0] += w[i] * y[i];

				// dG/dg
				A[6, 6] += SQR(z[i]);
				A[6, 7] += z[i];

				b[6, 0] += w[i] * z[i];

				//dG/dh
				//A[7, 7] += 1;

				b[7, 0] += w[i];
			}

			// copy duplicit values
			A[3, 4] = A[2, 5] = A[1, 6] = A[0, 7];
			A[1, 2] = A[0, 4];
			A[1, 3] = A[0, 5];
			A[2, 3] = A[0, 6];
			A[7, 7] = n; //dG/dh

			// fill oposite diagonal with sum(xyz)
			for (int i = 1; i < 4; i++)
			{
				A[i, 7 - i] = A[0, 7];
			}
			
			// make matrix symmetric
			for (int i = 0; i < 8; i++)
			{
				for (int j = i + 1; j < 8; j++)
				{
					A[j, i] = A[i, j];
				}
			}

			return new TrilinearForm(solve(A, b));
		}

		public static TrilinearIrregularForm DoTrilinearIrregularInterpolation(IList<float> x, IList<float> y, IList<float> z, IList<float> w)
		{
			int n = x.Count;
			Debug.Assert(x.Count == n && y.Count == n && z.Count == n && w.Count == n);
			Debug.Assert(n >= GetMinNumberOfDataPoints(ApproximationMethod.TrilinearInterpolation)); /**/

			Vector3[] vertexCoords = new Vector3[8];
			float[] vertexValues = new float[8];

			float[] distances = { float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue };

			// compute bounding box
			Vector3 lowerBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 upperBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue);

			for (int index = 0; index < n; index++)
			{
				lowerBounds.X = Math.Min(lowerBounds.X, x[index]);
				lowerBounds.Y = Math.Min(lowerBounds.Y, y[index]);
				lowerBounds.Z = Math.Min(lowerBounds.Z, z[index]);
				upperBounds.X = Math.Max(upperBounds.X, x[index]);
				upperBounds.Y = Math.Max(upperBounds.Y, y[index]);
				upperBounds.Z = Math.Max(upperBounds.Z, z[index]);
			}

			Vector3[] corners = getCornerPositions(ref lowerBounds, ref upperBounds);

			for (int index = 0; index < n; index++)
			{
				Vector3 dataPos = new Vector3(x[index], y[index], z[index]);

				for (int i = 0; i < 8; i++)
				{
					float dist = (corners[i] - dataPos).Length;
					if (dist < distances[i])
					{
						vertexCoords[i] = dataPos;
						vertexValues[i] = w[index];
						distances[i] = dist;
					}
				}
			}

			return new TrilinearIrregularForm(vertexCoords, vertexValues);
		}

		private static Vector3[] getCornerPositions(ref Vector3 lowerBounds, ref Vector3 upperBounds)
		{
			Vector3[] corners = new Vector3[8];
			corners[0] = upperBounds; // XYZ
			corners[1] = new Vector3(upperBounds.X, upperBounds.Y, lowerBounds.Z); // XY-Z
			corners[2] = new Vector3(lowerBounds.X, upperBounds.Y, lowerBounds.Z); // -XY-Z
			corners[3] = new Vector3(lowerBounds.X, upperBounds.Y, upperBounds.Z); // -XYZ
			corners[4] = new Vector3(upperBounds.X, lowerBounds.Y, upperBounds.Z); // X-YZ
			corners[5] = new Vector3(upperBounds.X, lowerBounds.Y, lowerBounds.Z); // X-Y-Z
			corners[6] = lowerBounds; // -X-Y-Z
			corners[7] = new Vector3(lowerBounds.X, lowerBounds.Y, upperBounds.Z); // -X-YZ
			return corners;
		}

		/// <summary>
		/// Least squares quadrilinear interpolation. (Includes time component)
		/// </summary>
		/// <param name="x">x component</param>
		/// <param name="y">y component</param>
		/// <param name="z">z component</param>
		/// <param name="t">time component</param>
		/// <param name="w">data values</param>
		public static QuadrilinearForm DoQuadrilinearInterpolation(IList<float> x, IList<float> y, IList<float> z, IList<float> t, IList<float> w)
		{
			int n = x.Count;
			Debug.Assert(x.Count == n && y.Count == n && z.Count == n && w.Count == n);

			Matrix A = new Matrix(16, 16);
			Matrix b = new Matrix(16, 1);

			// w = axyzt + bxyz + cxyt + dxzt + eyzt + fxy + gxz + hxt + iyz + jyt + kzt + lx + my + nz + ot + p

			// assemble matrix
			for (int i = 0; i < n; i++)
			{
				// a
				A[0, 0] += SQR(t[i]) * SQR(x[i]) * SQR(y[i]) * SQR(z[i]);
				A[0, 1] += t[i] * SQR(x[i]) * SQR(y[i]) * SQR(z[i]);
				A[0, 2] += SQR(t[i]) * SQR(x[i]) * SQR(y[i]) * z[i];
				A[0, 3] += SQR(t[i]) * SQR(x[i]) * y[i] * SQR(z[i]);
				A[0, 4] += SQR(t[i]) * x[i] * SQR(y[i]) * SQR(z[i]);
				A[0, 5] += t[i] * SQR(x[i]) * SQR(y[i]) * z[i];
				A[0, 6] += t[i] * SQR(x[i]) * y[i] * SQR(z[i]);
				A[0, 7] += SQR(t[i]) * SQR(x[i]) * y[i] * z[i];
				A[0, 8] += t[i] * x[i] * SQR(y[i]) * SQR(z[i]);
				A[0, 9] += SQR(t[i]) * x[i] * SQR(y[i]) * z[i];
				A[0, 10] += SQR(t[i]) * x[i] * y[i] * SQR(z[i]);
				A[0, 11] += t[i] * SQR(x[i]) * y[i] * z[i];
				A[0, 12] += t[i] * x[i] * SQR(y[i]) * z[i];
				A[0, 13] += t[i] * x[i] * y[i] * SQR(z[i]);
				A[0, 14] += SQR(t[i]) * x[i] * y[i] * z[i];
				A[0, 15] += t[i] * x[i] * y[i] * z[i];

				b[0, 0] += w[i] * t[i] * x[i] * y[i] * z[i];

				// b
				A[1, 1] += SQR(x[i]) * SQR(y[i]) * SQR(z[i]);
				A[1, 2] += t[i] * SQR(x[i]) * SQR(y[i]) * z[i];
				A[1, 3] += t[i] * SQR(x[i]) * y[i] * SQR(z[i]);
				A[1, 4] += t[i] * x[i] * SQR(y[i]) * SQR(z[i]);
				A[1, 5] += SQR(x[i]) * SQR(y[i]) * z[i];
				A[1, 6] += SQR(x[i]) * y[i] * SQR(z[i]);
				A[1, 7] += t[i] * SQR(x[i]) * y[i] * z[i];
				A[1, 8] += x[i] * SQR(y[i]) * SQR(z[i]);
				A[1, 9] += t[i] * x[i] * SQR(y[i]) * z[i];
				A[1, 10] += t[i] * x[i] * y[i] * SQR(z[i]);
				A[1, 11] += SQR(x[i]) * y[i] * z[i];
				A[1, 12] += x[i] * SQR(y[i]) * z[i];
				A[1, 13] += x[i] * y[i] * SQR(z[i]);
				//A[1, 14] += t[i] * x[i] * y[i] * z[i];
				A[1, 15] += x[i] * y[i] * z[i];

				b[1, 0] += w[i] * x[i] * y[i] * z[i];

				// c
				A[2, 2] += SQR(t[i]) * SQR(x[i]) * SQR(y[i]);
				A[2, 3] += SQR(t[i]) * SQR(x[i]) * y[i] * z[i];
				A[2, 4] += SQR(t[i]) * x[i] * SQR(y[i]) * z[i];
				A[2, 5] += t[i] * SQR(x[i]) * SQR(y[i]);
				A[2, 6] += t[i] * SQR(x[i])* y[i] * z[i];
				A[2, 7] += SQR(t[i]) * SQR(x[i]) * y[i];
				A[2, 8] += t[i] * x[i] * SQR(y[i]) * z[i];
				A[2, 9] += SQR(t[i]) * x[i] * SQR(y[i]);
				A[2, 10] += SQR(t[i]) * x[i] * y[i] * z[i];
				A[2, 11] += t[i] * SQR(x[i])* y[i];
				A[2, 12] += t[i] * x[i] * SQR(y[i]);
				//A[2, 13] += t[i] * x[i] * y[i] * z[i];
				A[2, 14] += SQR(t[i]) * x[i] * y[i];
				A[2, 15] += t[i] * x[i] * y[i];

				b[2, 0] += w[i] * t[i] * x[i] * y[i];

				// d
				A[3, 3] += SQR(t[i]) * SQR(x[i]) * SQR(z[i]);
				A[3, 4] += SQR(t[i]) * x[i] * y[i] * SQR(z[i]);
				A[3, 5] += t[i] * SQR(x[i]) * y[i] * z[i];
				A[3, 6] += t[i] * SQR(x[i]) * SQR(z[i]);
				A[3, 7] += SQR(t[i]) * SQR(x[i]) * z[i];
				A[3, 8] += t[i] * x[i] * y[i] * SQR(z[i]);
				A[3, 9] += SQR(t[i]) * x[i] * y[i] * z[i];
				A[3, 10] += SQR(t[i]) * x[i] * SQR(z[i]);
				A[3, 11] += t[i] * SQR(x[i]) * z[i];
				//A[3, 12] += t[i] * x[i] * y[i] * z[i];
				A[3, 13] += t[i] * x[i] * SQR(z[i]);
				A[3, 14] += SQR(t[i]) * x[i] * z[i];
				A[3, 15] += t[i] * x[i] * z[i];

				b[3, 0] += w[i] * t[i] * x[i] * z[i];

				// e
				A[4, 4] += SQR(t[i]) * SQR(y[i]) * SQR(z[i]);
				A[4, 5] += t[i] * x[i] * SQR(y[i]) * z[i];
				A[4, 6] += t[i] * x[i] * y[i] * SQR(z[i]);
				A[4, 7] += SQR(t[i]) * x[i] * y[i] * z[i];
				A[4, 8] += t[i] * SQR(y[i]) * SQR(z[i]);
				A[4, 9] += SQR(t[i]) * SQR(y[i]) * z[i];
				A[4, 10] += SQR(t[i]) * y[i] * SQR(z[i]);
				//A[4, 11] += t[i] * x[i] * y[i] * z[i];
				A[4, 12] += t[i] * SQR(y[i]) * z[i];
				A[4, 13] += t[i] * y[i] * SQR(z[i]);
				A[4, 14] += SQR(t[i]) * y[i] * z[i];
				A[4, 15] += t[i] * y[i] * z[i];

				b[4, 0] += w[i] * t[i] * y[i] * z[i];

				// f
				A[5, 5] += SQR(x[i]) * SQR(y[i]);
				A[5, 6] += SQR(x[i]) * y[i] * z[i];
				A[5, 7] += t[i] * SQR(x[i]) * y[i];
				A[5, 8] += x[i] * SQR(y[i]) * z[i];
				A[5, 9] += t[i] * x[i] * SQR(y[i]);
				//A[5, 10] += t[i] * x[i] * y[i] * z[i];
				A[5, 11] += SQR(x[i]) * y[i];
				A[5, 12] += x[i] * SQR(y[i]);
				A[5, 13] += x[i] * y[i] * z[i];
				A[5, 14] += t[i] * x[i] * y[i];
				A[5, 15] += x[i] * y[i];

				b[5, 0] += w[i] * x[i] * y[i];

				// g
				A[6, 6] += SQR(x[i]) * SQR(z[i]);
				A[6, 7] += t[i] * SQR(x[i]) * z[i];
				A[6, 8] += x[i] * y[i] * SQR(z[i]);
				//A[6, 9] += t[i] * x[i] * y[i] * z[i];
				A[6, 10] += t[i] * x[i] * SQR(z[i]);
				A[6, 11] += SQR(x[i]) * z[i];
				A[6, 12] += x[i] * y[i] * z[i];
				A[6, 13] += x[i] * SQR(z[i]);
				A[6, 14] += t[i] * x[i] * z[i];
				A[6, 15] += x[i] * z[i];

				b[6, 0] += w[i] * x[i] * z[i];

				// h
				A[7, 7] += SQR(t[i]) * SQR(x[i]);
				//A[7, 8] += t[i] * x[i] * y[i] * z[i];
				A[7, 9] += SQR(t[i]) * x[i] * y[i];
				A[7, 10] += SQR(t[i]) * x[i] * z[i];
				A[7, 11] += t[i] * SQR(x[i]);
				A[7, 12] += t[i] * x[i] * y[i];
				A[7, 13] += t[i] * x[i] * z[i];
				A[7, 14] += SQR(t[i]) * x[i];
				A[7, 15] += t[i] * x[i];

				b[7, 0] += w[i] * t[i] * x[i];

				// i
				A[8, 8] += SQR(y[i]) * SQR(z[i]);
				A[8, 9] += t[i] * SQR(y[i]) * z[i];
				A[8, 10] += t[i] * y[i] * SQR(z[i]);
				A[8, 11] += x[i] * y[i] * z[i];
				A[8, 12] += SQR(y[i]) * z[i];
				A[8, 13] += y[i] * SQR(z[i]);
				A[8, 14] += t[i] * y[i] * z[i];
				A[8, 15] += y[i] * z[i];

				b[8, 0] += w[i] * y[i] * z[i];

				// j
				A[9, 9] += SQR(t[i]) * SQR(y[i]);
				A[9, 10] += SQR(t[i]) * y[i] * z[i];
				A[9, 11] += t[i] * x[i] * y[i];
				A[9, 12] += t[i] * SQR(y[i]);
				A[9, 13] += t[i] * y[i] * z[i];
				A[9, 14] += SQR(t[i]) * y[i];
				A[9, 15] += t[i] * y[i];

				b[9, 0] += w[i] * t[i] * y[i];

				// k
				A[10, 10] += SQR(t[i]) * SQR(z[i]);
				A[10, 11] += t[i] * x[i] * z[i];
				A[10, 12] += t[i] * y[i] * z[i];
				A[10, 13] += t[i] * SQR(z[i]);
				A[10, 14] += SQR(t[i]) * z[i];
				A[10, 15] += t[i] * z[i];

				b[10, 0] += w[i] * t[i] * z[i];

				// l
				A[11, 11] += SQR(x[i]);
				A[11, 12] += x[i] * y[i];
				A[11, 13] += x[i] * z[i];
				A[11, 14] += t[i] * x[i];
				A[11, 15] += x[i];

				b[11, 0] += w[i] * x[i];

				// m
				A[12, 12] += SQR(y[i]);
				A[12, 13] += y[i] * z[i];
				A[12, 14] += t[i] * y[i];
				A[12, 15] += y[i];

				b[12, 0] += w[i] * y[i];

				// n
				A[13, 13] += SQR(z[i]);
				A[13, 14] += t[i] * z[i];
				A[13, 15] += z[i];

				b[13, 0] += w[i] * z[i];

				// o
				A[14, 14] += SQR(t[i]);
				A[14, 15] += t[i];

				b[14, 0] += w[i] * t[i];

				// p
				b[15, 0] += w[i];
			}

			A[15, 15] = n;

			// fill oposite diagonal with sum(txyz)
			for (int i = 1; i < 8; i++)
			{
				A[i, 15 - i] = A[0, 15];
			}

			// make matrix symmetric
			for (int i = 0; i < 16; i++)
			{
				for (int j = i + 1; j < 16; j++)
				{
					A[j, i] = A[i, j];
				}
			}

			return new QuadrilinearForm(solve(A, b));
		}

		#region Helper methods

		public static float SQR(float x)
		{
			return x * x;
		}

		public static float CUB(float x)
		{
			return x * x * x;
		}

		public static float BIQ(float x)
		{
			return x * x * x * x;
		}

		#endregion

		#region Matrix operations

		private static float[] solve(Matrix A, Matrix b)
		{
			int[] indexMap;
			bool removed = removeZeroRowsAndColumns(ref A, ref b, out indexMap);

			Matrix u = null;
			try
			{
				u = Matrix.SolveLinear(A, b); // solve system Au = b
			}
			catch (MatrixSingularException)
			{
				Debug.Assert(false);

				//if (n >= GetMinNumberOfDataPoints(ApproximationMethod.LinearRegression))
				//	return DoLinearRegression(x, y, z, w);
				//return new ConstValue((float)b[7, 0] / n);

				return new float[A.NoRows]; // return zero vector
			}

			Debug.Assert(!Matrix.ReferenceEquals(u, null));

			if (removed)
			{
				return getMatrixColumn(u, 0, indexMap);
			}
			return getMatrixColumn(u, 0);
		}

		private static float[] getMatrixColumn(Matrix m, int columnIndex)
		{
			float[] column = new float[m.NoRows];
			for (int i = 0; i < m.NoRows; i++)
			{
				column[i] = (float)m[i, columnIndex];
			}
			return column;
		}

		private static float[] getMatrixColumn(Matrix m, int columnIndex, int[] indexMap)
		{
			float[] column = new float[indexMap.Length];
			for (int i = 0; i < indexMap.Length; i++)
			{
				if (indexMap[i] != -1)
					column[i] = (float)m[indexMap[i], columnIndex];
				else
					column[i] = 0f; // set default value
			}
			return column;
		}

		private static bool removeZeroRowsAndColumns(ref Matrix A, ref Matrix b, out int[] indexMap)
		{
			Debug.Assert(A.NoRows == A.NoCols && A.NoRows == b.NoRows && b.NoCols == 1);
			int index = 0;
			indexMap = new int[A.NoRows];
			for (int i = 0; i < A.NoRows; i++)
			{
				if (isRowZero(b, i) && isRowZero(A, i)/* && isColumnZero(A, i)*/) // check for column or not??
				{
					Debug.Assert(isColumnZero(A, i));
					indexMap[i] = -1; // remove row
				}
				else
				{
					indexMap[i] = index++;
				}
			}

			if (index != A.NoRows) // need to compress matrices
			{
				Matrix newA = new Matrix(index, index);
				Matrix newB = new Matrix(index, 1);

				for (int i = 0; i < indexMap.Length; i++)
				{
					if (indexMap[i] != -1)
					{
						for (int j = 0; j < indexMap.Length; j++)
						{
							if (indexMap[j] != -1)
							{
								newA[indexMap[i], indexMap[j]] = A[i, j]; // copy A to newA
							}
						}
						newB[indexMap[i], 0] = b[i, 0]; // copy b to newB
					}
				}

				// replace original matrices
				A = newA;
				b = newB;

				return true;
			}
			return false;
		}

		private static bool isRowZero(Matrix m, int rowIndex)
		{
			for (int i = 0; i < m.NoCols; i++)
			{
				if (m[rowIndex, i] != 0.0) // add epsilon equality?
					return false;
			}
			return true;
		}

		private static bool isColumnZero(Matrix m, int columnIndex)
		{
			for (int i = 0; i < m.NoRows; i++)
			{
				if (m[i, columnIndex] != 0.0) // add epsilon equality?
					return false;
			}
			return true;
		}

		#endregion

	}
}
