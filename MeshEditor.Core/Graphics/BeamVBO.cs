using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// trida pro reprezentaci Vertex Buffer Object urceneho pro zobrazeni mnoziny beamu (1D usecek)
	/// </summary>
    public class BeamVBO : IVertexBufferObject
    {

        #region Fields, Constructor, Properties

        private int vertexCount;
        private int vertexBufferID, colorBufferID;

        public BeamVBO(int count, IEnumerable<Beam> beams, bool beamPropertyColors)
        {
            vertexCount = 0;
            vertexBufferID = colorBufferID = 0;
            createBuffer(count, beams, beamPropertyColors);
        }

		public int ColorBufferID
		{
			get { return colorBufferID; }
		}

		public int VertexBufferID
		{
			get { return vertexBufferID; }
		}

        #endregion

        #region Private methods

        private void createBuffer(int count, IEnumerable<Beam> beams, bool beamPropertyColors)
        {
            this.vertexCount = count * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            int[] colors = new int[vertexCount];
            int index = 0;
            int color = Utilities.Functions.ColorToRgba32(Scene.BeamColor);
            foreach (Beam beam in beams)
            {
				if (beamPropertyColors)
					color = PropertyColorProvider.GetRGBA32(beam.Property);
                vertices[index] = beam.BeginNode.Position;
				colors[index++] = color;
				vertices[index] = beam.EndNode.Position;
				colors[index++] = color;
            }

            // generovani vertex bufferu
			if (vertices.Length > 0)
			{
				GL.GenBuffers(1, out vertexBufferID);
				GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
				int vertexBufferSize = vertices.Length * Vector3.SizeInBytes;
				GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)vertexBufferSize, vertices, BufferUsageHint.StaticDraw);
				GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			}

            // generovani face-color bufferu
			if (colors.Length > 0)
			{
				GL.GenBuffers(1, out colorBufferID);
				GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferID);
				int faceColorBufferSize = colors.Length * sizeof(int);
				GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)faceColorBufferSize, colors, BufferUsageHint.StaticDraw/**/);
				GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			}
        }

        #endregion
        
        #region Public methods

		public void Draw()
		{
			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
			//----------------------------------------------------
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
			GL.EnableClientState(ArrayCap.VertexArray);
			
			GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferID);
			GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);
			GL.EnableClientState(ArrayCap.ColorArray);

			GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			//----------------------------------------------------
			GL.PopClientAttrib();
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

        public void Dispose()
        {
			GL.DeleteBuffers(1, ref vertexBufferID);
			GL.DeleteBuffers(1, ref colorBufferID);
			vertexBufferID = colorBufferID = 0;
			vertexCount = 0;
        }

        #endregion

	}
}
