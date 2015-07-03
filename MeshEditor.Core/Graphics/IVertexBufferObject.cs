using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.Graphics
{
	public interface IVertexBufferObject : IDisposable
	{
		bool MapBuffer(BufferTarget target, int bufferID, BufferAccess access, out IntPtr videoMemoryPointer);
	}
}
