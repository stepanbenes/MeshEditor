using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Color selection dialog (counterpart of System.Windows.Forms.ColorDialog).
	/// </summary>
	public partial class ColorPickerDialog : Window
	{
		private bool dialogResultOK;

		public ColorPickerDialog()
		{
			InitializeComponent();
		}

		public Color Color
		{
			get => colorView.Color;
			set => colorView.Color = value;
		}

		/// <summary>
		/// Shows the dialog modally; true when a color was chosen (DialogResult.OK).
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

		private void buttonOK_Click(object sender, RoutedEventArgs e)
		{
			dialogResultOK = true;
			Close();
		}

		private void pageTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (colorView != null && pageTabs.SelectedIndex >= 0)
				colorView.SelectedIndex = pageTabs.SelectedIndex;
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		public static Color FromDrawingColor(System.Drawing.Color color) => Color.FromArgb(color.A, color.R, color.G, color.B);

		public static System.Drawing.Color ToDrawingColor(Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
	}
}
