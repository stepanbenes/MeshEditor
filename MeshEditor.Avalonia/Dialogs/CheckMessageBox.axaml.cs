using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Message box with a "Don't show this again" check box (port of MeshEditor.WinUI.CheckMessageBox).
	/// </summary>
	public partial class CheckMessageBox : Window
	{
		private bool isChecked;

		public CheckMessageBox()
			: this(string.Empty, string.Empty)
		{ }

		public CheckMessageBox(string message, string caption)
		{
			InitializeComponent();
			isChecked = false;
			this.Title = caption;
			label1.Text = message;
			checkBox1.Content = "Don't show this again";
		}

		public bool IsChecked
		{
			get => isChecked;
			set => checkBox1.IsChecked = isChecked = value;
		}

		public new async Task ShowDialog(Window owner)
		{
			owner ??= DialogOwner.Current;
			if (owner != null)
				await base.ShowDialog(owner);
			else
			{
				var closed = new TaskCompletionSource();
				Closed += (s, e) => closed.TrySetResult();
				Show();
				await closed.Task;
			}
		}

		private void checkBox1_CheckedChanged(object sender, RoutedEventArgs e)
		{
			isChecked = checkBox1.IsChecked == true;
		}

		private void buttonOK_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
