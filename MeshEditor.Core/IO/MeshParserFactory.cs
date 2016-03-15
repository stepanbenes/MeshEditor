using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MeshEditor.IO
{
	public static class MeshParserFactory
	{
		public static IMeshFileParser Create(string filename)
		{
			Debug.Assert(filename != null);

			var extension = Path.GetExtension(filename).ToLower();
			// pick the right loader according to filename extension
			switch (extension)
			{
				case ".msh": // GiD mesh 
					return new GiDMshFileFormatParser(filename);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlMeshParser(filename);
				case ".json":
					return new LayerMeshFileParser(filename);

				case ".ply":
					return new PLYFileFormatParser(filename);
				case ".obj":
					return new OBJFileFormatParser(filename);

				default:
					return new SifelFileFormatParser(filename);
			}
		}

		public static IMeshFileParser Create(string[] filenames)
		{
			Debug.Assert(filenames != null && filenames.Length > 0);

			if (filenames == null || filenames.Length == 0)
				throw new ArgumentException("filenames");

			if (filenames.Length == 1)
			{
				return Create(filenames[0]);
			}
			else
			{
				return new MultipleFilesParser(filenames);
			}
		}
	}
}
