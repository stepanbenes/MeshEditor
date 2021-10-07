using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.Graphics
{
	public class VBO : IVertexBufferObject
	{

		#region Fields, constructor, Properties

		private int vertexCount;
		private int vertexBufferID, colorBufferID, normalBufferID;

		private PrimitiveType primitivesType;

		public VBO(PrimitiveType primitivesType, Vector3[] vertices, int[] colors = null, Vector3[] normals = null)
		{
			this.primitivesType = primitivesType;
			this.vertexCount = vertices.Length;
			createBuffer(vertices, colors, normals);
		}

		#endregion

		#region Public methods

		public void Draw(PrimitiveType primitiveType, bool bindColors = true, bool bindNormals = true)
		{
			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
			//----------------------------------------------------
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			GL.VertexPointer(3, VertexPointerType.Float, 0, 0);
			GL.EnableClientState(ArrayCap.VertexArray);

			if (bindColors && colorBufferID > 0)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferID);
				GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, 0);
				GL.EnableClientState(ArrayCap.ColorArray);
			}

			if (bindNormals && normalBufferID > 0)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, normalBufferID);
				GL.NormalPointer(NormalPointerType.Float, 0, 0);
				GL.EnableClientState(ArrayCap.NormalArray);
			}

			// draw
			GL.DrawArrays(primitiveType, 0, vertexCount);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			//----------------------------------------------------
			GL.PopClientAttrib();
		}

		public void Draw()
		{
			Draw(this.primitivesType);
		}

		public void Draw(IndexBufferObject ibo, bool bindColors, bool bindNormals)
		{
			Debug.Assert(ibo != null);

			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
			//----------------------------------------------------
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
			GL.EnableClientState(ArrayCap.VertexArray);

			if (bindColors && colorBufferID > 0)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferID);
				GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);
				GL.EnableClientState(ArrayCap.ColorArray);
			}

			if (bindNormals && normalBufferID > 0)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, normalBufferID);
				GL.NormalPointer(NormalPointerType.Float, 0, IntPtr.Zero);
				GL.EnableClientState(ArrayCap.NormalArray);
			}

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo.BufferID);
			//GL.IndexPointer(IndexPointerType.Int, 0, IntPtr.Zero);
			//GL.EnableClientState(EnableCap.IndexArray);

			GL.DrawElements(ibo.PrimitiveType, ibo.ElementCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//----------------------------------------------------
			GL.PopClientAttrib();
		}

		public void ChangeColors(IEnumerable<int> newColors)
		{
			IntPtr videoMemory;

			if (!MapBuffer(BufferTarget.ArrayBuffer, colorBufferID, BufferAccess.WriteOnly, out videoMemory))
				return;

			unsafe
			{
				int index = 0;
				int* items = (int*)videoMemory.ToPointer();
				foreach (int color in newColors)
					items[index++] = color;
			}

			if (!GL.UnmapBuffer(BufferTarget.ArrayBuffer))
			{
#if DEBUG
				throw new Exception("Error while unmapping buffer.");
#else
				Console.WriteLine("Error while unmapping buffer.");
#endif
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void ChangeColors(IEnumerable<KeyValuePair<int, int>> indexColorPairs)
		{
			IntPtr videoMemory;

			if (!MapBuffer(BufferTarget.ArrayBuffer, colorBufferID, BufferAccess.WriteOnly, out videoMemory))
				return;

			unsafe
			{
				int* items = (int*)videoMemory.ToPointer();
				foreach (var indexColor in indexColorPairs)
					items[indexColor.Key] = indexColor.Value;
			}

			if (!GL.UnmapBuffer(BufferTarget.ArrayBuffer))
			{
#if DEBUG
				throw new Exception("Error while unmapping buffer.");
#else
				Console.WriteLine("Error while unmapping buffer.");
#endif
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void ChangeColors(int uniformColor)
		{
			IntPtr videoMemory;

			if (!MapBuffer(BufferTarget.ArrayBuffer, colorBufferID, BufferAccess.WriteOnly, out videoMemory))
				return;

			unsafe
			{
				int* items = (int*)videoMemory.ToPointer();
				for (int i = 0; i < vertexCount; i++)
				{
					items[i] = uniformColor;
				}
			}

			if (!GL.UnmapBuffer(BufferTarget.ArrayBuffer))
			{
#if DEBUG
				throw new Exception("Error while unmapping buffer.");
#else
				Console.WriteLine("Error while unmapping buffer.");
#endif
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void Dispose()
		{
			if (vertexBufferID > 0)
			{
				GL.DeleteBuffers(1, ref vertexBufferID);
				vertexBufferID = 0;
			}
			if (colorBufferID > 0)
			{
				GL.DeleteBuffers(1, ref colorBufferID);
				colorBufferID = 0;
			}
			if (normalBufferID > 0)
			{
				GL.DeleteBuffers(1, ref normalBufferID);
				normalBufferID = 0;
			}
			vertexCount = 0;
		}

		public bool MapBuffer(BufferTarget target, int bufferID, BufferAccess access, out IntPtr videoMemoryPointer)
		{
			//try
			//{
			if (bufferID <= 0)
			{
				videoMemoryPointer = IntPtr.Zero;
				return false;
			}
			GL.BindBuffer(target, bufferID);
			videoMemoryPointer = GL.MapBuffer(target, access);
			return true;
			//}
			//catch // Do not catch exeptions, here I have no options to deal with it
			//{
			//	videoMemoryPointer = IntPtr.Zero;
			//	return false;
			//}
		}

		#endregion

		#region Private methods

		private void createBuffer(Vector3[] vertices, int[] colors = null, Vector3[] normals = null)
		{
			Debug.Assert(vertices != null);
			Debug.Assert(colors == null || vertices.Length == colors.Length);
			Debug.Assert(normals == null || vertices.Length == normals.Length);

			// generate vertex buffer
			if (vertices.Length > 0)
			{
				GL.GenBuffers(1, out vertexBufferID);
				GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
				int vertexBufferSize = vertices.Length * Vector3.SizeInBytes;
				GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)vertexBufferSize, vertices, BufferUsageHint.StaticDraw);
			}

			// generate color buffer
			if (colors != null && colors.Length > 0)
			{
				GL.GenBuffers(1, out colorBufferID);
				GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferID);
				int colorBufferSize = colors.Length * sizeof(int);
				GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)colorBufferSize, colors, BufferUsageHint.DynamicDraw/**/);
			}

			// generate normal buffer
			if (normals != null && normals.Length > 0)
			{
				Debug.Assert(vertices.Length == normals.Length);
				GL.GenBuffers(1, out normalBufferID);
				GL.BindBuffer(BufferTarget.ArrayBuffer, normalBufferID);
				int normalBufferSize = normals.Length * Vector3.SizeInBytes;
				GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)normalBufferSize, normals, BufferUsageHint.StaticDraw);
			}

			// unbind
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		#endregion

	}
}
