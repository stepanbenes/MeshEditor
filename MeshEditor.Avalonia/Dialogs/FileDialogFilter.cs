using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Converts WinForms file dialog filters ("Description|*.a;*.b|...") used by the core to Avalonia file types.
	/// </summary>
	internal static class FileDialogFilter
	{
		public static List<FilePickerFileType> Parse(string filter)
		{
			var result = new List<FilePickerFileType>();
			if (string.IsNullOrEmpty(filter))
				return result;
			string[] parts = filter.Split('|');
			for (int i = 0; i + 1 < parts.Length; i += 2)
			{
				result.Add(new FilePickerFileType(parts[i].Trim())
				{
					Patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
				});
			}
			return result;
		}
	}
}
