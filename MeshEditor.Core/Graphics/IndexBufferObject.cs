using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// trida reprezentujici Index Buffer Object pridruze k hlavnimu Vertex Buffer Object.
	/// VBO je jen jeden pro celou sit. IBO je zvlast pro plochy, ostre hrany, ostatni hrany, uzly, beamy.
	/// </summary>
	public class IndexBufferObject : IDisposable
	{
		#region Fields, Properties, Constructor

		private int bufferID;
		private int elementCount;

		private PrimitiveType primitiveType;

		public PrimitiveType PrimitiveType
		{
			get { return primitiveType; }
		}

		public int BufferID
		{
			get { return bufferID; }
		}

		//public int SizeInBytes
		//{
		//    get { return sizeInBytes; }
		//}

		public int ElementCount
		{
			get { return elementCount; }
		}

		public IndexBufferObject(PrimitiveType primitiveType, int[] indices)
		{
			this.primitiveType = primitiveType;
			createBuffer(indices);
		}

		#endregion

		#region Methods

		private void createBuffer(int[] indices)
		{
			this.elementCount = indices.Length;
			// generovani index bufferu
			if (indices.Length > 0)
			{
				GL.GenBuffers(1, out bufferID);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, bufferID);
				int sizeInBytes = indices.Length * sizeof(int);
				GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)sizeInBytes, indices, BufferUsageHint.StaticDraw);
				int size;
				GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
				if (size != sizeInBytes)
					throw new ApplicationException("Index array not uploaded correctly");
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			}
		}

		public void Dispose()
		{
			if (bufferID > 0)
			{
				GL.DeleteBuffers(1, ref bufferID);
				bufferID = 0;
			}
		}

		#endregion
	}
}
