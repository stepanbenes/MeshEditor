using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Mathematics
{
	/// <summary>
	/// Represents algebraic expression.
	/// </summary>
	public abstract class Polynomial
	{
		public abstract float ComputeValue(float x, float y, float z);

		//public virtual float ComputeValue(float x, float y, float z, float t)
		//{
		//	return ComputeValue(x, y, z);
		//}

		public abstract float[] GetParameters();

		protected abstract Polynomial CreateWithParameters(float[] parameters);

		private Polynomial doOperationWith(Polynomial other, Func<float, float, float> operation)
		{
			Debug.Assert(other != null && this.GetType() == other.GetType() && operation != null);
			float[] p1 = this.GetParameters();
			float[] p2 = other.GetParameters();
			Debug.Assert(p1.Length == p2.Length);
			float[] result = new float[p1.Length];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = operation(p1[i], p2[i]);
			}
			return CreateWithParameters(result);
		}

		public static Polynomial Interpolate(Polynomial a, Polynomial b, float position)
		{
			Debug.Assert(position >= 0f && position <= 1f);
			return a.doOperationWith(b, (p1, p2) => (p2 - p1) * position + p1);
		}

		public abstract int SizeInBytes { get; }

		//public bool IsMultipleOf(Polynomial other, float error, out float factor)
		//{
		//	Debug.Assert(other != null && error >= 0f && this.GetType() == other.GetType());
		//	if (this.GetType() != other.GetType())
		//	{
		//		factor = float.NaN;
		//		return false;
		//	}
		//	return parametersHaveCommonMultiple(this.GetParameters(), other.GetParameters(), error, out factor);
		//}

		//private static bool parametersHaveCommonMultiple(float[] p1, float[] p2, float error, out float factor)
		//{
		//	Debug.Assert(p1.Length == p2.Length);
		//	factor = float.NaN;
		//	for (int i = 0; i < p1.Length; i++)
		//	{
		//		float f = p1[i] / p2[i];
		//		if (i > 0 && Math.Abs(f - factor) > error)
		//			return false;
		//		factor = f;
		//	}
		//	return true;
		//}

		//public Polynomial Minus(Polynomial other)
		//{
		//	return this.DoOperationWith(other, (p1, p2) => p1 - p2);
		//}
	}

	public class ConstantValue : Polynomial
	{
		float value;

		public ConstantValue(float value)
		{
			this.value = value;
		}

		public override float ComputeValue(float x, float y, float z)
		{
			return value;
		}

		public override float[] GetParameters()
		{
			return new float[] { value };
		}

		protected override Polynomial CreateWithParameters(float[] parameters)
		{
			Debug.Assert(parameters != null && parameters.Length == 1);
			return new ConstantValue(parameters[0]);
		}

		public override int SizeInBytes
		{
			get { return sizeof(float); }
		}
	}

	public class HyperQuadric : Polynomial
	{
		// quadric parameters
		float a, b, c, d, e, f, g, h, i, j;

		public HyperQuadric(float[] paramsArray)
		{
			Debug.Assert(paramsArray != null && paramsArray.Length == 10);
			this.a = paramsArray[0];
			this.b = paramsArray[1];
			this.c = paramsArray[2];
			this.d = paramsArray[3];
			this.e = paramsArray[4];
			this.f = paramsArray[5];
			this.g = paramsArray[6];
			this.h = paramsArray[7];
			this.i = paramsArray[8];
			this.j = paramsArray[9];
		}

		public HyperQuadric(float a, float b, float c, float d, float e, float f, float g, float h, float i, float j)
		{
			this.a = a;
			this.b = b;
			this.c = c;
			this.d = d;
			this.e = e;
			this.f = f;
			this.g = g;
			this.h = h;
			this.i = i;
			this.j = j;
		}

		public override float ComputeValue(float x, float y, float z)
		{
			// w = ax^2 + by^2 + cz^2 + dxy + exz + fyz + gx + hy + iz + j
			return a * x * x + b * y * y + c * z * z + d * x * y + e * x * z + f * y + z + g * x + h * y + i * z + j;
		}

		public override float[] GetParameters()
		{
			return new float[] { a, b, c, d, e, f, g, h, i, j };
		}

		protected override Polynomial CreateWithParameters(float[] parameters)
		{
			return new HyperQuadric(parameters);
		}

		public override int SizeInBytes
		{
			get { return 10 * sizeof(float); }
		}
	}

	public class HyperPlane : Polynomial
	{
		// plane parameters
		float a, b, c, d;

		public HyperPlane(float[] paramsArray)
		{
			Debug.Assert(paramsArray != null && paramsArray.Length == 4);
			this.a = paramsArray[0];
			this.b = paramsArray[1];
			this.c = paramsArray[2];
			this.d = paramsArray[3];
		}

		public HyperPlane(float a, float b, float c, float d)
		{
			this.a = a;
			this.b = b;
			this.c = c;
			this.d = d;
		}

		public override float ComputeValue(float x, float y, float z)
		{
			// w = ax + by + cz + d
			return a * x + b * y + c * z + d;
		}

		public override float[] GetParameters()
		{
			return new float[] { a, b, c, d };
		}

		protected override Polynomial CreateWithParameters(float[] parameters)
		{
			return new HyperPlane(parameters);
		}

		public override int SizeInBytes
		{
			get { return 4 * sizeof(float); }
		}
	}

	public class TrilinearForm : Polynomial
	{
		float a, b, c, d, e, f, g, h;

		public TrilinearForm(float a, float b, float c, float d, float e, float f, float g, float h)
		{
			this.a = a;
			this.b = b;
			this.c = c;
			this.d = d;
			this.e = e;
			this.f = f;
			this.g = g;
			this.h = h;
		}

		public TrilinearForm(float[] paramsArray)
		{
			Debug.Assert(paramsArray != null && paramsArray.Length == 8);
			this.a = paramsArray[0];
			this.b = paramsArray[1];
			this.c = paramsArray[2];
			this.d = paramsArray[3];
			this.e = paramsArray[4];
			this.f = paramsArray[5];
			this.g = paramsArray[6];
			this.h = paramsArray[7];
		}

		public override float ComputeValue(float x, float y, float z)
		{
			// w = axyz + bxy + cxz + dyz + ex + fy + gz + h
			return a * x * y * z + b * x * y + c * x * z + d * y * z + e * x + f * y + g * z + h;
		}

		public override float[] GetParameters()
		{
			return new float[] { a, b, c, d, e, f, g, h };
		}

		protected override Polynomial CreateWithParameters(float[] parameters)
		{
			return new TrilinearForm(parameters);
		}

		public override int SizeInBytes
		{
			get { return 8 * sizeof(float); }
		}
	}

	public class TrilinearIrregularForm : Polynomial
	{
		Vector3[] vertexCoords;
		float[] vertexValues;

		public TrilinearIrregularForm(Vector3[] vertexCoords, float[] vertexValues)
		{
			Debug.Assert(vertexCoords != null && vertexValues != null);
			this.vertexCoords = vertexCoords;
			this.vertexValues = vertexValues;
		}

		public override float ComputeValue(float x, float y, float z)
		{
			Vector3 xyz = new Vector3(x, y, z);
			return doTrilinearExtrapolation(ref xyz, vertexCoords, vertexValues);
		}

		public override float[] GetParameters()
		{
			return vertexValues;
		}

		public static int[,] XEdgeIndexes = { { 6, 5 }, { 2, 1 }, { 7, 4 }, { 3, 0 } };
		public static int[,] YEdgeIndexes = { { 6, 2 }, { 5, 1 }, { 7, 3 }, { 4, 0 } };
		public static int[,] ZEdgeIndexes = { { 6, 7 }, { 5, 4 }, { 2, 3 }, { 1, 0 } };

		private static float doTrilinearExtrapolation(ref Vector3 dataPos, Vector3[] vertexCoords, float[] vertexValues)
		{
			Debug.Assert(vertexCoords != null && vertexCoords.Length == 8 && vertexValues != null && vertexValues.Length == 8);

			float diff;

			// X-Edges
			float[] xValues = new float[4];
			Vector3[] interPosX = new Vector3[4];
			for (int i = 0; i < 4; i++)
			{
				int firstCorner = XEdgeIndexes[i, 0];
				int secondCorner = XEdgeIndexes[i, 1];
				xValues[i] = interpolateTwoValues(vertexCoords[firstCorner].X, vertexCoords[secondCorner].X, vertexValues[firstCorner], vertexValues[secondCorner], dataPos.X, out diff);
				interPosX[i] = interpolateTwoPoints(ref vertexCoords[firstCorner], ref vertexCoords[secondCorner], diff);
			}

			// Y-Edges
			float[] yValues = new float[2];
			Vector3[] interPosY = new Vector3[2];
			for (int i = 0; i < 2; i++)
			{
				yValues[i] = interpolateTwoValues(interPosX[i * 2].Y, interPosX[i * 2 + 1].Y, xValues[i * 2], xValues[i * 2 + 1], dataPos.Y, out diff);
				interPosY[i] = interpolateTwoPoints(ref interPosX[i * 2], ref interPosX[i * 2 + 1], diff);
			}

			// Z-Edge
			float zValue = interpolateTwoValues(interPosY[0].Z, interPosY[1].Z, yValues[0], yValues[1], dataPos.Z, out diff);

			return zValue;
		}

		private static float interpolateTwoValues(float posA, float posB, float valA, float valB, float posX, out float diff)
		{
			float denom = posB - posA;
			if (Math.Abs(denom) < Common.Epsilon)
				diff = 0.5f;
			else
				diff = (posX - posA) / denom;
			return valA * (1f - diff) + valB * diff;
		}

		private static Vector3 interpolateTwoPoints(ref Vector3 pointA, ref Vector3 pointB, float diff)
		{
			return pointA + (pointB - pointA) * diff;
		}

		protected override Polynomial CreateWithParameters(float[] parameters)
		{
			return new TrilinearIrregularForm(this.vertexCoords, parameters);
		}

		public override int SizeInBytes
		{
			get { return vertexCoords.Length * Vector3.SizeInBytes + vertexValues.Length * sizeof(float) + IntPtr.Size * 2; }
		}
	}

	public class QuadrilinearForm : Polynomial
	{
		float[] p;

		/// <summary>
		/// Creates instance of quadrilinear form. Requires array of 16 parameters as input argument.
		/// </summary>
		/// <param name="parameters">Array of 16 floating-point numbers.</param>
		public QuadrilinearForm(float[] parameters)
		{
			Debug.Assert(parameters != null && parameters.Length == 16);
			this.p = parameters;
		}

		public override float ComputeValue(float x, float y, float z)
		{
			throw new NotSupportedException();
		}

		public /*override*/ float ComputeValue(float x, float y, float z, float t)
		{
			// w = axyzt + bxyz + cxyt + dxzt + eyzt + fxy + gxz + hxt + iyz + jyt + kzt + lx + my + nz + ot + p

			return
				p[0] * x * y * z * t +	// a
				p[1] * x * y * z +		// b
				p[2] * x * y * t +		// c
				p[3] * x * z * t +		// d
				p[4] * y * z * t +		// e
				p[5] * x * y +			// f
				p[6] * x * z +			// g
				p[7] * x * t +			// h
				p[8] * y * z +			// i
				p[9] * y * t +			// j
				p[10] * z * t +			// k
				p[11] * x +				// l
				p[12] * y +				// m
				p[13] * z +				// n
				p[14] * t +				// o
				p[15];					// p
		}

		public override float[] GetParameters()
		{
			return p;
		}

		protected override Polynomial CreateWithParameters(float[] parameters)
		{
			return new QuadrilinearForm(parameters);
		}

		public override int SizeInBytes
		{
			get { return p.Length * sizeof(float) + IntPtr.Size; }
		}
	}
}
