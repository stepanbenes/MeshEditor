using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Common
{
	public static class StringExtensions
	{
		public static string MakeUniqueFilename(this string prefix)
		{
			return string.Format("{0}_{1}", MakeAlphanumericFilename(prefix), Guid.NewGuid().ToString());
		}

		public static string MakeAlphanumericFilename(this string filename)
		{
			Regex rgx = new Regex("[^a-zA-Z0-9]");
			return rgx.Replace(filename, "_");
		}

		public static string MakeValidFilename(this string filename)
		{
			StringBuilder text = new StringBuilder(filename);
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				text.Replace(c, '_');
			}
			return text.ToString();
		}
	}
}
