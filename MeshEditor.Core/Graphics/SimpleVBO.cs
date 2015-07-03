using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// trida zapouzdrujici Vertex Buffer Object pro libovolne pouziti
	/// </summary>
	public class SimpleVBO : IDisposable
	{
		private int vertexCount;
		private int vertexBufferID;

		public SimpleVBO(Vector3[] vertices)
		{
			this.vertexCount = vertices.Length;
			createBuffer(vertices);
		}

		private void createBuffer(Vector3[] vertices)
		{
			// generovani vertex bufferu
			GL.GenBuffers(1, out vertexBufferID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			int vertexBufferSize = vertices.Length * Vector3.SizeInBytes;
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)vertexBufferSize, vertices, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void Draw(BeginMode mode)
		{
			//----------------------------------------------------
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.VertexArray);

			GL.DrawArrays(mode, 0, vertexCount);

			GL.DisableClientState(EnableCap.VertexArray);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			//----------------------------------------------------
		}

		public void Dispose()
		{
			GL.DeleteBuffers(1, ref vertexBufferID);
			vertexBufferID = 0;
			vertexCount = 0;
		}

	}
}
