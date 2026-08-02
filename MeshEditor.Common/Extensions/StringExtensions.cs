using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MeshEditor.Common.Extensions
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

		public static string[] SplitToLines(this string text, bool removeEmptyLines)
		{
			return text.Split(new[] { "\r\n", "\n" }, options: removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
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

			var normalized = text.Normalize(NormalizationForm.FormD);
			var builder = new StringBuilder(normalized.Length);
			foreach (var ch in normalized)
			{
				var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
				if (unicodeCategory != UnicodeCategory.NonSpacingMark)
				{
					builder.Append(ch);
				}
			}

			return builder.ToString().Normalize(NormalizationForm.FormC);
		}

		public static string TrimOrExtendToLength(this string text, int length)
		{
			if (length < 0)
				throw new ArgumentException(nameof(length));
			if (string.IsNullOrEmpty(text))
				return new string(' ', length);
			if (text.Length == length)
				return text;
			if (text.Length < length)
				return text.PadRight(length);
			const string elipsis = "...";
			if (length < elipsis.Length)
				return text.Substring(0, length);
			return text.Substring(0, length - elipsis.Length) + elipsis;
		}
	}
}
