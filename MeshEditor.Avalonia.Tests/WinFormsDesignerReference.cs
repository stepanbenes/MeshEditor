using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Reads menu/tool strip definitions from a WinForms *.Designer.cs file, which is the reference for UI parity.
	/// </summary>
	internal sealed class WinFormsDesignerReference
	{
		private readonly string source;

		public WinFormsDesignerReference(string path)
		{
			source = File.ReadAllText(path);
		}

		public static WinFormsDesignerReference MainForm =>
			new WinFormsDesignerReference(Path.Combine(AppContext.BaseDirectory, "Reference", "MainForm.Designer.cs.txt"));

		/// <summary>
		/// Child item names of a menu / tool strip in their order ("-" for separators).
		/// </summary>
		public IReadOnlyList<string> GetChildren(string parentName)
		{
			var match = Regex.Match(source, @"this\." + Regex.Escape(parentName) + @"\.(?:DropDownItems|Items)\.AddRange\(new System\.Windows\.Forms\.ToolStripItem\[\] \{(.*?)\}\);", RegexOptions.Singleline);
			if (!match.Success)
				return Array.Empty<string>();
			return Regex.Matches(match.Groups[1].Value, @"this\.(\w+)")
				.Select(m => IsSeparator(m.Groups[1].Value) ? "-" : m.Groups[1].Value)
				.ToList();
		}

		public bool HasChildren(string name) => GetChildren(name).Count > 0;

		public static bool IsSeparator(string name) => name.StartsWith("toolStripSeparator", StringComparison.Ordinal);

		public string GetText(string name) => getStringProperty(name, "Text");

		public string GetToolTipText(string name) => getStringProperty(name, "ToolTipText");

		public bool IsVisible(string name) => !Regex.IsMatch(source, @"this\." + Regex.Escape(name) + @"\.Visible = false;");

		/// <summary>
		/// Shortcut in Avalonia KeyGesture notation (e.g. "Ctrl+Alt+S"), or null.
		/// </summary>
		public string GetShortcut(string name)
		{
			var match = Regex.Match(source, @"this\." + Regex.Escape(name) + @"\.ShortcutKeys = (.*?);", RegexOptions.Singleline);
			if (!match.Success)
				return null;
			var keys = Regex.Matches(match.Groups[1].Value, @"Keys\.(\w+)").Select(m => m.Groups[1].Value).ToList();
			var modifiers = new List<string>();
			if (keys.Remove("Control")) modifiers.Add("Ctrl");
			if (keys.Remove("Shift")) modifiers.Add("Shift");
			if (keys.Remove("Alt")) modifiers.Add("Alt");
			string key = keys.Single();
			return string.Join("+", modifiers.Append(key));
		}

		/// <summary>
		/// WinForms "&amp;File" -> Avalonia "_File".
		/// </summary>
		public static string ToAvaloniaAccessText(string text) => text?.Replace("&&", "\u0001").Replace("&", "_").Replace("\u0001", "&");

		private string getStringProperty(string name, string property)
		{
			var match = Regex.Match(source, @"this\." + Regex.Escape(name) + @"\." + property + @" = ""((?:[^""\\]|\\.)*)""(?:\s*\+\s*""((?:[^""\\]|\\.)*)"")*;", RegexOptions.Singleline);
			if (!match.Success)
				return null;
			string value = match.Groups[1].Value + string.Concat(match.Groups[2].Captures.Select(c => c.Value));
			return Regex.Unescape(value);
		}
	}
}
