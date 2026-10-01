using Avalonia;
using Avalonia.Controls;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Input value dialog with a check box (port of MeshEditor.WinUI.CheckedInputValueForm).
	/// </summary>
	public class CheckedInputValueDialog : InputValueDialog
	{
		private readonly CheckBox checkBox;

		public CheckedInputValueDialog()
			: this("Input value", string.Empty, "is checked")
		{ }

		public CheckedInputValueDialog(string caption, string infoText, string checkBoxText)
			: this(caption, infoText, SavedValue, checkBoxText)
		{ }

		public CheckedInputValueDialog(string caption, string infoText, string boxValue, string checkBoxText)
			: base(caption, infoText, boxValue)
		{
			checkBox = new CheckBox
			{
				Name = "checkBox",
				Content = checkBoxText,
				Margin = new Thickness(0, -6, 0, 6),
			};
			ContentAboveValue = checkBox;
		}

		public bool IsChecked
		{
			get => checkBox.IsChecked == true;
			set => checkBox.IsChecked = value;
		}

		internal CheckBox CheckBox => checkBox;
	}
}
