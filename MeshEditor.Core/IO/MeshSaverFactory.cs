using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MeshEditor.IO
{
	public static class MeshSaverFactory
	{
		public static IMeshSaver Create(string filename)
		{
			Debug.Assert(filename != null);

			// pick the right saver according to filename extension
			switch (Path.GetExtension(filename).ToLower())
			{
				case ".msh":
					return new GiDMshFileFormatSaver();
				case ".vtk":
					return new VTKFileFormatSaver();
				default:
					return new DefaultFileFormatMeshSaver();
			}
		}
	}
}
