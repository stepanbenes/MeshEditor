using System;
using System.Collections.Generic;
using System.IO;
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

		/// <summary>
		/// Extension (without the dot) of the first concrete pattern of the file type, null for "*.*".
		/// </summary>
		public static string GetExtension(FilePickerFileType fileType)
		{
			string pattern = fileType?.Patterns?.FirstOrDefault(p => p.StartsWith("*.") && p.IndexOfAny(new[] { '*', '?' }, 2) < 0);
			return pattern?.Substring(2);
		}

		/// <summary>
		/// Appends the extension of the selected file type to a file name typed without one (SaveFileDialog.AddExtension in WinForms).
		/// Not all platform pickers do it themselves.
		/// </summary>
		public static string AddExtension(string fileName, FilePickerFileType selectedType)
		{
			string extension = GetExtension(selectedType);
			if (fileName == null || extension == null || Path.HasExtension(fileName))
				return fileName;
			return fileName + "." + extension;
		}
	}
}
