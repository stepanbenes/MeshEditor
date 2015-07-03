using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.IO
{
	public static class DataParserFactory
	{
		public static IDataFileParser Create(string filename, long fileStartPosition = 0)
		{
			Debug.Assert(!string.IsNullOrEmpty(filename));
			if (!File.Exists(filename))
				throw new DataLoadingException(string.Format("Specified file path does not exists!"), filename);

			switch (Path.GetExtension(filename))
			{
				case ".res":
					return new GiDResFileFormatParser(filename, fileStartPosition);
				default:
					throw new DataLoadingException("This data format is not supported.", filename);
			}
		}
	}
}
