using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MeshEditor.IO;

namespace MeshEditor.DataVisualizer.IO
{
	public static class DataParserFactory
	{
		public static IDataFileParser Create(string filename, long fileStartPosition = 0)
		{
			Debug.Assert(!string.IsNullOrEmpty(filename));
			if (!File.Exists(filename))
			{
				throw new FileParserException(string.Format("Specified file path does not exists!"), filename);
			}

			string extension = Path.GetExtension(filename).ToLower();
			switch (extension)
			{
				case ".res":
					return new GiDResFileParser(filename, fileStartPosition);
				case ".vtu": // VTK XML, only serial UnstructuredGrid (.vtu) is supported
					return new VTKXmlDataFileParser(filename, time: null);
				case ".pvd": // ParaView Data file format, collection of pointers to VTK files
					return new ParaViewDataFileParser(filename);
				case ".json":
					return new JsonDataFileParser(filename);

				default:
					throw new FileParserException($"This data format is not supported ({extension}).", filename);
            }
		}
	}
}
