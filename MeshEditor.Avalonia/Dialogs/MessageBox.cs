using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	public enum MessageBoxButtons { OK, OKCancel, YesNo, YesNoCancel }

	public enum MessageBoxIcon { None, Information, Question, Warning, Error }

	public enum MessageBoxResult { None, OK, Cancel, Yes, No }

	/// <summary>
	/// Cross-platform replacement of System.Windows.Forms.MessageBox (Avalonia has no built-in message box).
	/// </summary>
	public sealed class MessageBox : Window
	{
		private MessageBoxResult result = MessageBoxResult.None;
		private readonly MessageBoxResult escapeResult;

		private MessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, int defaultButtonIndex)
		{
			Title = caption ?? string.Empty;
			Name = "MessageBox";
			CanResize = false;
			ShowInTaskbar = false;
			SizeToContent = SizeToContent.WidthAndHeight;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			MinWidth = 260;
			MaxWidth = 700;

			var buttonResults = buttons switch
			{
				MessageBoxButtons.OKCancel => new[] { MessageBoxResult.OK, MessageBoxResult.Cancel },
				MessageBoxButtons.YesNo => new[] { MessageBoxResult.Yes, MessageBoxResult.No },
				MessageBoxButtons.YesNoCancel => new[] { MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel },
				_ => new[] { MessageBoxResult.OK },
			};
			escapeResult = Array.IndexOf(buttonResults, MessageBoxResult.Cancel) >= 0 ? MessageBoxResult.Cancel
				: buttons == MessageBoxButtons.OK ? MessageBoxResult.OK : MessageBoxResult.None;

			var buttonPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				Spacing = 8,
			};
			var buttonControls = new List<Button>();
			foreach (var buttonResult in buttonResults)
			{
				var button = new Button
				{
					Content = buttonResult.ToString(),
					Name = "Button" + buttonResult,
					MinWidth = 80,
					HorizontalContentAlignment = HorizontalAlignment.Center,
				};
				var r = buttonResult;
				button.Click += (s, e) => { result = r; Close(); };
				buttonControls.Add(button);
				buttonPanel.Children.Add(button);
			}
			int defaultIndex = Math.Clamp(defaultButtonIndex, 0, buttonControls.Count - 1);
			buttonControls[defaultIndex].IsDefault = true;

			var textBlock = new SelectableTextBlock
			{
				Text = text ?? string.Empty,
				TextWrapping = TextWrapping.Wrap,
				VerticalAlignment = VerticalAlignment.Center,
				Name = "MessageText",
			};

			var body = new DockPanel { LastChildFill = true };
			var iconText = iconGlyph(icon);
			if (iconText != null)
			{
				var iconBlock = new Border
				{
					Width = 32,
					Height = 32,
					CornerRadius = new CornerRadius(16),
					Background = iconBrush(icon),
					Margin = new Thickness(0, 0, 12, 0),
					VerticalAlignment = VerticalAlignment.Top,
					Child = new TextBlock
					{
						Text = iconText,
						Foreground = Brushes.White,
						FontWeight = FontWeight.Bold,
						FontSize = 18,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
					},
				};
				DockPanel.SetDock(iconBlock, Dock.Left);
				body.Children.Add(iconBlock);
			}
			body.Children.Add(textBlock);

			var root = new DockPanel { Margin = new Thickness(16) };
			buttonPanel.Margin = new Thickness(0, 16, 0, 0);
			DockPanel.SetDock(buttonPanel, Dock.Bottom);
			root.Children.Add(buttonPanel);
			root.Children.Add(body);
			Content = root;

			Opened += (s, e) => buttonControls[defaultIndex].Focus();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape && escapeResult != MessageBoxResult.None)
			{
				result = escapeResult;
				e.Handled = true;
				Close();
				return;
			}
			base.OnKeyDown(e);
		}

		private static string iconGlyph(MessageBoxIcon icon) => icon switch
		{
			MessageBoxIcon.Information => "i",
			MessageBoxIcon.Question => "?",
			MessageBoxIcon.Warning => "!",
			MessageBoxIcon.Error => "×",
			_ => null,
		};

		private static IBrush iconBrush(MessageBoxIcon icon) => icon switch
		{
			MessageBoxIcon.Information => new SolidColorBrush(Color.FromRgb(0x1E, 0x6F, 0xD9)),
			MessageBoxIcon.Question => new SolidColorBrush(Color.FromRgb(0x1E, 0x6F, 0xD9)),
			MessageBoxIcon.Warning => new SolidColorBrush(Color.FromRgb(0xE0, 0x9B, 0x00)),
			MessageBoxIcon.Error => new SolidColorBrush(Color.FromRgb(0xD1, 0x34, 0x38)),
			_ => Brushes.Transparent,
		};

		/// <summary>
		/// Shows a modal message box.
		/// </summary>
		/// <param name="defaultButtonIndex">Zero-based index of the default button (like MessageBoxDefaultButton.Button1..3).</param>
		public static async Task<MessageBoxResult> Show(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK,
			MessageBoxIcon icon = MessageBoxIcon.None, int defaultButtonIndex = 0, Window owner = null)
		{
			var box = new MessageBox(text, caption, buttons, icon, defaultButtonIndex);
			owner ??= DialogOwner.Current;
			if (owner != null)
				await box.ShowDialog(owner);
			else
			{
				var closed = new TaskCompletionSource();
				box.Closed += (s, e) => closed.TrySetResult();
				box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
				box.Show();
				await closed.Task;
			}
			if (box.result == MessageBoxResult.None)
				box.result = box.escapeResult == MessageBoxResult.None ? MessageBoxResult.Cancel : box.escapeResult;
			return box.result;
		}
	}

	/// <summary>
	/// Resolves the window that should own modal dialogs (the active window, or the main window).
	/// </summary>
	internal static class DialogOwner
	{
		public static Window Current
		{
			get
			{
				if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				{
					foreach (var window in desktop.Windows)
						if (window.IsActive && window.IsVisible)
							return window;
					return desktop.MainWindow;
				}
				return null;
			}
		}
	}
}
