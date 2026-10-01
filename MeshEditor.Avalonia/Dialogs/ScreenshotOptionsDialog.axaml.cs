using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Choice between a screenshot of the whole scene or of a selected area (port of MeshEditor.WinUI.ScreenshotOptionsForm).
	/// </summary>
	public partial class ScreenshotOptionsDialog : Window
	{
		private static bool savedUseSelectionAreaState;

		private bool dialogResultOK;

		public ScreenshotOptionsDialog()
		{
			InitializeComponent();
			UseSelectionArea = savedUseSelectionAreaState;
			updateOkButtonText();
		}

		public bool UseSelectionArea
		{
			get => radioButtonSelectionArea.IsChecked == true;
			set
			{
				if (value)
					radioButtonSelectionArea.IsChecked = true;
				else
					radioButtonWholeScene.IsChecked = true;
			}
		}

		/// <summary>
		/// Shows the dialog modally; true when confirmed (DialogResult.OK).
		/// </summary>
		public new async Task<bool> ShowDialog(Window owner)
		{
			owner ??= DialogOwner.Current;
			if (owner != null)
				await base.ShowDialog(owner);
			else
			{
				var closed = new TaskCompletionSource();
				Closed += (s, e) => closed.TrySetResult();
				WindowStartupLocation = WindowStartupLocation.CenterScreen;
				Show();
				await closed.Task;
			}
			return dialogResultOK;
		}

		private void radioButtonWholeScene_CheckedChanged(object sender, RoutedEventArgs e)
		{
			updateOkButtonText();
		}

		private void updateOkButtonText()
		{
			buttonOK.Content = UseSelectionArea ? "Select" : "Save";
		}

		private void buttonOK_Click(object sender, RoutedEventArgs e)
		{
			savedUseSelectionAreaState = UseSelectionArea;
			dialogResultOK = true;
			Close();
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
