using System.Threading.Tasks;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Placeholder for WinForms dialogs that have not been ported to Avalonia yet.
	/// Every call site names the original form so the remaining work is easy to find (search for "NotPorted.Show").
	/// </summary>
	internal static class NotPorted
	{
		public static Task Show(string featureName, string originalFormName)
		{
			return MessageBox.Show(
				$"'{featureName}' is not available in the cross-platform UI yet." + System.Environment.NewLine +
				$"(The WinForms dialog '{originalFormName}' has not been ported to Avalonia.)",
				"Not ported yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
