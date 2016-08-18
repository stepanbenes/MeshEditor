using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MeshEditor.Graphics
{
	public abstract class ShaderHolder : IDisposable
	{
		protected List<int> vertexShaderId;
		protected List<int> fragmentShaderId;

		private int programShaderId;

		private int activeVertexShaderIndex, activeFragmentShaderIndex;

		static int counter;

		public ShaderHolder()
		{
			Console.WriteLine("creating shaderholder, count: " + (++counter));

			vertexShaderId = new List<int>();
			fragmentShaderId = new List<int>();
			
			programShaderId = -1;

			activeVertexShaderIndex = activeFragmentShaderIndex = -1;
		}

		protected int Program
		{
			get { return programShaderId; }
		}

		protected bool LoadShaderStrings(string[] vertexShaderStrings, string[] fragmentShaderStrings)
		{
			const int badResult = -1;

			if (vertexShaderStrings != null)
			{
				foreach (string vs in vertexShaderStrings)
				{
					if (AddVertexShaderString(vs) == badResult)
						return false;
				}
			}

			if (fragmentShaderStrings != null)
			{
				foreach (string fs in fragmentShaderStrings)
				{
					if (AddFragmentShaderString(fs) == badResult)
						return false;
				}
			}

			return true;
		}

		private int AddVertexShaderString(string vertexShaderString)
		{
			int vId = CreateVertexShader(vertexShaderString);
			if (!CheckShader(vId))
				return -1;

			vertexShaderId.Add(vId);
			return vId;
		}

		private int AddFragmentShaderString(string fragmentShaderString)
		{
			int fId = CreateFragmentShader(fragmentShaderString);
			if (!CheckShader(fId))
				return -1;

			fragmentShaderId.Add(fId);
			return fId;
		}

		protected bool SetActiveShaders(int vertexShaderIndex, int fragmentShaderIndex)
		{
			if (programShaderId == -1)
			{
				programShaderId = GL.CreateProgram();
			}

			bool change = false;
			if (activeVertexShaderIndex != vertexShaderIndex)
			{
				if (activeVertexShaderIndex != -1)
					GL.DetachShader(programShaderId, vertexShaderId[activeVertexShaderIndex]);
				GL.AttachShader(programShaderId, vertexShaderId[vertexShaderIndex]);
				activeVertexShaderIndex = vertexShaderIndex;
				change = true;
			}
			if (activeFragmentShaderIndex != fragmentShaderIndex)
			{
				if (activeFragmentShaderIndex != -1)
					GL.DetachShader(programShaderId, fragmentShaderId[activeFragmentShaderIndex]);
				GL.AttachShader(programShaderId, fragmentShaderId[fragmentShaderIndex]);
				activeFragmentShaderIndex = fragmentShaderIndex;
				change = true;
			}

			if (!change)
				return true;

			GL.LinkProgram(programShaderId);

			return CheckProgram(programShaderId);
		}

		private int CreateVertexShader(string vertexShaderString)
		{
			int shaderId = GL.CreateShader(ShaderType.VertexShader);
			GL.ShaderSource(shaderId, vertexShaderString);
			GL.CompileShader(shaderId);

			return shaderId;
		}

		private int CreateFragmentShader(string fragmentShaderString)
		{
			int shaderId = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(shaderId, fragmentShaderString);
			GL.CompileShader(shaderId);

			return shaderId;
		}

		private bool CheckShader(int shaderId)
		{
			int res = -1;
			GL.GetShader(shaderId, ShaderParameter.CompileStatus, out res);
			if (res != 1)
			{
				string infoLog;
				GL.GetShaderInfoLog(shaderId, out infoLog);
				Debug.WriteLine(infoLog);
				return false;
			}
			return true;
		}

		private bool CheckProgram(int programId)
		{
			int res = -1;
			GL.GetProgram(programId, ProgramParameter.LinkStatus, out res);
			if (res != 1)
			{
				string infoLog;
				GL.GetProgramInfoLog(programId, out infoLog);
				Debug.WriteLine(infoLog);
				return false;
			}
			return true;
		}

		#region IDisposable

		public void Dispose()
		{
			Console.WriteLine("disposing shaderholder, count: " + (--counter));

			// get rid of resources
			if (programShaderId != -1)
			{
				if (activeVertexShaderIndex >= 0)
					GL.DetachShader(programShaderId, vertexShaderId[activeVertexShaderIndex]);
				foreach (int id in vertexShaderId)
					GL.DeleteShader(id);
				if (activeFragmentShaderIndex >= 0)
					GL.DetachShader(programShaderId, fragmentShaderId[activeFragmentShaderIndex]);
				foreach (int id in fragmentShaderId)
					GL.DeleteShader(id);
				GL.DeleteProgram(programShaderId);
			}
		}

		#endregion

	}
}
