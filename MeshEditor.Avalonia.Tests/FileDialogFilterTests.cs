using MeshEditor.AvaloniaUI.Dialogs;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Save pickers append the extension of the selected file type like SaveFileDialog.AddExtension did in WinForms.
	/// </summary>
	public class FileDialogFilterTests
	{
		private static readonly string meshFilter = "SIFEL file format (*.t3d)|*.t3d|GiD mesh file format (*.msh)|*.msh|All files (*.*)|*.*";

		[Test]
		public void Extension_of_the_selected_file_type_is_appended_to_a_name_without_one()
		{
			var fileTypes = FileDialogFilter.Parse(meshFilter);
			Assert.That(FileDialogFilter.AddExtension("/tmp/out", fileTypes[1]), Is.EqualTo("/tmp/out.msh"));
			Assert.That(FileDialogFilter.AddExtension("/tmp/out", fileTypes[0]), Is.EqualTo("/tmp/out.t3d"));
		}

		[Test]
		public void Typed_extension_is_kept()
		{
			var fileTypes = FileDialogFilter.Parse(meshFilter);
			Assert.That(FileDialogFilter.AddExtension("/tmp/out.vtk", fileTypes[1]), Is.EqualTo("/tmp/out.vtk"));
		}

		[Test]
		public void Nothing_is_appended_for_all_files_or_no_selection()
		{
			var fileTypes = FileDialogFilter.Parse(meshFilter);
			Assert.That(FileDialogFilter.AddExtension("/tmp/out", fileTypes[2]), Is.EqualTo("/tmp/out"));
			Assert.That(FileDialogFilter.AddExtension("/tmp/out", null), Is.EqualTo("/tmp/out"));
			Assert.That(FileDialogFilter.AddExtension(null, fileTypes[0]), Is.Null);
		}
	}
}
