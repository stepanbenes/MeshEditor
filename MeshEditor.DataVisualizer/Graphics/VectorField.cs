using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MeshEditor.Graphics;
using MeshEditor.DataVisualizer.Mathematics;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.DataVisualizer.Graphics
{
	public class VectorField : IDisposable
	{
		readonly VBO linesVBO;
		readonly VBO arrowsVBO;

		public decimal LengthFactor { get; }
		public bool InvertVectorArrows { get; }

		public VectorField(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector3> vectors, float minDistanceBetweenPoints, double scale, decimal lengthFactor, bool invertVectorArrows)
		{
			Debug.Assert(positions != null && vectors != null);
			Debug.Assert(positions.Count == vectors.Count);
			Debug.Assert(scale > 0.0);
			Debug.Assert(lengthFactor > 0m);

			LengthFactor = lengthFactor;
			InvertVectorArrows = invertVectorArrows;

			float resizeFactor = (float)((double)lengthFactor * scale);
			createBuffers(positions, vectors, minDistanceBetweenPoints, resizeFactor, invertVectorArrows, out linesVBO, out arrowsVBO);
		}

		private static void createBuffers(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector3> vectors, float minDistanceBetweenPoints, float resizeFactor, bool moveEndOfArrowsToNodes, out VBO linesVBO, out VBO arrowsVBO)
		{
			Vector3[] vertices = new Vector3[positions.Count * 2];
			Vector3[] arrowVertices = new Vector3[positions.Count * 4 * 3];

			for (int i = 0; i < positions.Count; i++)
			{
				if (moveEndOfArrowsToNodes)
				{
					vertices[i * 2] = positions[i] - vectors[i] * resizeFactor;
					vertices[i * 2 + 1] = positions[i];
				}
				else
				{
					vertices[i * 2] = positions[i];
					vertices[i * 2 + 1] = positions[i] + vectors[i] * resizeFactor;
				}

				Vector3[] arrowCap = getArrowCap(minDistanceBetweenPoints, ref vertices[i * 2], ref vertices[i * 2 + 1]);
				Debug.Assert(arrowCap.Length == 4);
				for (int ai = 0; ai < 4; ai++)
				{
					arrowVertices[i * 12 + ai * 3] = vertices[i * 2 + 1];
					arrowVertices[i * 12 + ai * 3 + 1] = arrowCap[ai];
					arrowVertices[i * 12 + ai * 3 + 2] = arrowCap[(ai + 1) % 4];
				}
			}

			linesVBO = new VBO(BeginMode.Lines, vertices);
			arrowsVBO = new VBO(BeginMode.Triangles, arrowVertices);
		}

		private static Vector3[] getArrowCap(float minDistanceBetweenPoints, ref Vector3 from, ref Vector3 to)
		{
			Vector3[] arrowCap = new Vector3[4];

			Vector3 a = to - from;
			float vLength = a.Length;

			Debug.Assert(!vLength.IsAlmostZero());

			a = a / vLength; // normalize

			Vector3 axis = getSuitableAxisToComputeCrossProductForVector(ref a);

			Vector3 b;
			Vector3.Cross(ref a, ref axis, out b);

			Vector3 c;
			Vector3.Cross(ref a, ref b, out c);

			float arrowCapLength = vLength * 0.2f;
			float arrowCapWidth = arrowCapLength * 0.4f;

			b *= arrowCapWidth;
			c *= arrowCapWidth;
			Vector3 endOfArrowCap = to - a * arrowCapLength;

			arrowCap[0] = endOfArrowCap + b;
			arrowCap[1] = endOfArrowCap + c;
			arrowCap[2] = endOfArrowCap - b;
			arrowCap[3] = endOfArrowCap - c;

			return arrowCap;

			Vector3 getSuitableAxisToComputeCrossProductForVector(ref Vector3 v)
			{
				float absX = Math.Abs(v.X);
				float absY = Math.Abs(v.Y);
				float absZ = Math.Abs(v.Z);
				if (absX >= absY && absX >= absZ) // X is dominant
				{
					return Vector3.UnitY;
				}
				else // Y or Z is dominant
				{
					return Vector3.UnitX;
				}
			}
		}

		public void Draw()
		{
			GL.Color3(MeshEditor.Data.Scene.VectorArrowsColor);

			GL.Disable(EnableCap.Lighting);

			GL.LineWidth(2f);
			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.LineSmooth);

			// DRAW LINES
			linesVBO.Draw();

			GL.Disable(EnableCap.LineSmooth);
			GL.Disable(EnableCap.Blend);

			// DRAW ARROWS
			arrowsVBO.Draw();
		}

		public void Dispose()
		{
			linesVBO.Dispose();
			arrowsVBO.Dispose();
		}
	}
}
