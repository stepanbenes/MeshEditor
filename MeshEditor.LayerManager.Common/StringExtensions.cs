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

		public static string MakeAlphanumeric(this string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return string.Empty;
			return Regex.Replace(filename.RemoveDiacritics(), @"[^a-zA-Z0-9]", "_");
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

		public static string[] SplitToTokensWithQuotes(this string text)
		{
			// parse correctly quoted tokens (enclosed by '"' characters); see: http://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
			return Regex.Matches(text, tokensWithQuotesRegexPattern)
				.Cast<Match>()
				.Select(m => m.Value.RemoveQuotes())
				.ToArray();
		}

		public static string RemoveQuotes(this string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;
			return text.Trim(quotesTrimChars);
		}

		public static string QuoteIfContainsWhiteSpace(this string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;
			if (text.Any(char.IsWhiteSpace))
				return "\"" + text + "\"";
			return text;
		}

		public static int? GetNumberAtTheEnd(this string text)
		{
			var match = Regex.Matches(text, @"\d+$").Cast<Match>().FirstOrDefault();
			if (match != null)
			{
				return int.Parse(match.Value);
			}
			return null;
		}

		public static string RemoveDiacritics(this string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;
			byte[] tempBytes = Encoding.GetEncoding("ISO-8859-8").GetBytes(text);
			return Encoding.UTF8.GetString(tempBytes, 0, tempBytes.Length);
		}
	}
}
