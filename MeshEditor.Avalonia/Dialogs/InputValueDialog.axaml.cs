using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	public class InputValueValidatingEventArgs : EventArgs
	{
		public bool Cancel { get; set; }
		public string ErrorCaption { get; set; }
		public string ErrorMessage { get; set; }

		/// <summary>
		/// Rejects the input; the dialog shows the error message (if any) and lets the user fix the value.
		/// </summary>
		public void Reject(string caption, string message)
		{
			Cancel = true;
			ErrorCaption = caption;
			ErrorMessage = message;
		}
	}

	/// <summary>
	/// Dialog for entering a value by the user (port of MeshEditor.WinUI.InputValueForm).
	/// </summary>
	public partial class InputValueDialog : Window
	{
		public event EventHandler<InputValueValidatingEventArgs> InputValueValidating;

		public static string SavedValue = "0";

		private bool dialogResultOK;

		public InputValueDialog()
			: this("Input value", string.Empty)
		{ }

		public InputValueDialog(string caption, string infoText)
			: this(caption, infoText, SavedValue)
		{ }

		public InputValueDialog(string caption, string infoText, string boxValue)
		{
			InitializeComponent();

			Title = caption;
			initializeText(infoText);
			textBoxValue.Text = boxValue;
			textBoxValue.AddHandler(KeyDownEvent, textBoxValue_KeyDown, RoutingStrategies.Tunnel);

			Opened += (s, e) => focusTextbox();
		}

		public string InputValue => textBoxValue.Text ?? string.Empty;

		/// <summary>
		/// Shows the dialog modally. Returns true when confirmed by OK / Enter (DialogResult.OK in the WinForms version).
		/// </summary>
		public new async Task<bool> ShowDialog(Window owner)
		{
			owner ??= DialogOwner.Current;
			if (owner != null)
			{
				WindowStartupLocation = WindowStartupLocation.CenterOwner;
				await base.ShowDialog(owner);
			}
			else
			{
				var closed = new TaskCompletionSource();
				Closed += (s, e) => closed.TrySetResult();
				Show();
				await closed.Task;
			}
			return dialogResultOK;
		}

		private void focusTextbox()
		{
			textBoxValue.Focus();
			textBoxValue.SelectAll();
			// focusing may reset the selection once the focus change is processed; select again afterwards
			Avalonia.Threading.Dispatcher.UIThread.Post(textBoxValue.SelectAll, Avalonia.Threading.DispatcherPriority.Input);
		}

		private void initializeText(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				labelText.Text = string.Empty;
				return;
			}
			string[] rows = text.Split('|');
			StringBuilder builder = new StringBuilder();
			builder.Append(rows[0].Trim());
			for (int i = 1; i < rows.Length; i++)
			{
				builder.AppendLine();
				builder.Append(rows[i].Trim());
			}
			labelText.Text = builder.ToString();
		}

		protected async Task<bool> validateInput()
		{
			var args = new InputValueValidatingEventArgs();
			InputValueValidating?.Invoke(this, args);
			if (!args.Cancel)
			{
				SavedValue = InputValue;
				return true;
			}
			if (!string.IsNullOrEmpty(args.ErrorMessage))
				await MessageBox.Show(args.ErrorMessage, args.ErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error, owner: this);
			return false;
		}

		private async Task confirm()
		{
			if (await validateInput())
			{
				dialogResultOK = true;
				Close();
			}
			else
				focusTextbox();
		}

		private async void buttonOK_Click(object sender, RoutedEventArgs e)
		{
			await confirm();
		}

		private async void textBoxValue_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				e.Handled = true;
				dialogResultOK = false;
				Close();
			}
			else if (e.Key == Key.Enter || e.Key == Key.Return)
			{
				e.Handled = true;
				await confirm();
			}
		}
	}
}
