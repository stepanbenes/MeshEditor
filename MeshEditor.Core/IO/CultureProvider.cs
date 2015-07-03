using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace MeshEditor.IO
{
	/// <summary>
	/// staticka trida poskytujici objekt pro specifikaci anglicke lokalizace
	/// </summary>
	public static class CultureProvider
	{
		private static CultureInfo englishCulture;

		static CultureProvider()
		{
			englishCulture = new CultureInfo("en-US"); // sets us language culture
		}

		public static CultureInfo EnglishCulture
		{
			get { return englishCulture; }
		}
	}
}
