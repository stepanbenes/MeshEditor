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

			// pick the right loader according to filename extension
			switch (Path.GetExtension(filename).ToLower())
			{
				case ".ply":
					return new PLYFileFormatParser(filename);
				case ".msh":
					return new GiDMshFileFormatParser(filename);
				case ".obj":
					return new OBJFileFormatParser(filename);
				default:
					return new DefaultFileFormatParser(filename);
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
