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
		VBO linesVBO;
		VBO arrowsVBO;

		public VectorField(Vector3[] positions, Vector3[] vectors, float resizeFactor, bool moveEndOfArrowsToNodes)
		{
			Debug.Assert(positions != null && vectors != null);
			Debug.Assert(positions.Length == vectors.Length);
			Debug.Assert(resizeFactor > 0f);

			createBuffers(positions, vectors, resizeFactor, moveEndOfArrowsToNodes);
		}

		private void createBuffers(Vector3[] positions, Vector3[] vectors, float resizeFactor, bool moveEndOfArrowsToNodes)
		{
			Vector3[] vertices = new Vector3[positions.Length * 2];
			Vector3[] arrowVertices = new Vector3[positions.Length * 4 * 3];

			for (int i = 0; i < positions.Length; i++)
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

				Vector3[] arrowCap = getArrowCap(ref vertices[i * 2], ref vertices[i * 2 + 1]);
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

		private Vector3[] getArrowCap(ref Vector3 from, ref Vector3 to)
		{
			Vector3[] arrowCap = new Vector3[4];

			Vector3 a = to - from;
			float vLength = a.Length;

			a.Normalize();
			Vector3 axis = (a.Y.IsAlmostZero() && a.Z.IsAlmostZero()) ? Vector3.UnitY : Vector3.UnitX;

			Vector3 b;
			Vector3.Cross(ref a, ref axis, out b);

			Vector3 c;
			Vector3.Cross(ref a, ref b, out c);

			b *= vLength * 0.02f;
			c *= vLength * 0.02f;
			Vector3 endArrow = from + a * (vLength * 0.95f);

			arrowCap[0] = endArrow + b;
			arrowCap[1] = endArrow + c;
			arrowCap[2] = endArrow - b;
			arrowCap[3] = endArrow - c;

			return arrowCap;
		}

		public void Draw()
		{
			GL.Color3(1f, 0f, 0f); // red

			GL.Disable(EnableCap.Lighting);

			GL.LineWidth(0.5f);
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
			if (linesVBO != null)
			{
				linesVBO.Dispose();
				linesVBO = null;
			}
			if (arrowsVBO != null)
			{
				arrowsVBO.Dispose();
				arrowsVBO = null;
			}
		}
	}
}
