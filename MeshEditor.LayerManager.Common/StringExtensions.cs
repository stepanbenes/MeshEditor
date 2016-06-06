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
		private static readonly string tokensWithQuotesRegexPattern = @"[\""].+?[\""]|[^ ]+";
		private static readonly char[] quotesTrimChars = { '"' };

		public static string MakeUniqueFilename(this string prefix)
		{
			return string.Format("{0}_{1}", MakeAlphanumericFilename(prefix), Guid.NewGuid().ToString());
		}

		public static string MakeAlphanumericFilename(this string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return string.Empty;
			Regex rgx = new Regex("[^a-zA-Z0-9]");
			return rgx.Replace(filename, "_");
		}

		public static string MakeValidFilename(this string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return string.Empty;
			StringBuilder text = new StringBuilder(filename);
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				text.Replace(c, '_');
			}
			return text.ToString();
		}

		public static string[] SplitToTokensWithQuotes(this string line)
		{
			// parse correctly quoted tokens (enclosed by '"' characters); see: http://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
			return Regex.Matches(line, tokensWithQuotesRegexPattern)
				.Cast<Match>()
				.Select(m => m.Value.Trim(quotesTrimChars))
				.ToArray();
		}
	}
}
