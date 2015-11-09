using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Graphics
{
	public class CrossHatchShader : ShaderHolder
	{
		public void Use()
		{
			throw new NotImplementedException();

			GL.UseProgram(Program);
		}

		public void Unuse()
		{
			GL.UseProgram(0);
		}
	}
}
