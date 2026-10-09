using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.Common.Logging;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// Error of a postprocessing task with message, and details (stack trace, log) shown on demand
	/// (port of MeshEditor.DataVisualizer.UI.ExceptionReportForm).
	/// </summary>
	public class ExceptionReportDialog : Window
	{
		internal readonly TextBox textBoxCaption = new TextBox { IsReadOnly = true, FontWeight = FontWeight.Bold, BorderThickness = new Thickness(0), TextWrapping = TextWrapping.Wrap };
		internal readonly TextBox textBoxExceptionMessage = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 60 };
		internal readonly TextBox textBoxStackTrace = new TextBox { IsReadOnly = true, AcceptsReturn = true, FontFamily = new FontFamily("Consolas, Menlo, DejaVu Sans Mono, monospace") };
		internal readonly ListBox listBoxLog = new ListBox();
		internal readonly HyperlinkButton linkLabelShowDetails = new HyperlinkButton { Content = "Show details", Padding = new Thickness(0) };
		internal readonly TabControl tabControlDetails;

		public ExceptionReportDialog(string taskName, Exception exception, MemoryLogger logger)
		{
			Debug.Assert(exception != null);
			Title = "Error report";
			Width = 600;
			SizeToContent = SizeToContent.Height;
			CanMinimize = CanMaximize = false;
			ShowInTaskbar = false;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			textBoxCaption.Text = "Error in processing task: " + taskName;
			textBoxExceptionMessage.Text = exception.Message;
			textBoxStackTrace.Text = exception.StackTrace;
			if (logger != null)
				listBoxLog.ItemsSource = logger.GetRecordHistory().Cast<object>().ToList();

			tabControlDetails = new TabControl
			{
				Height = 220,
				IsVisible = false, // collapsed (Panel2Collapsed)
				Items =
				{
					new TabItem { Header = "Stack trace", Content = textBoxStackTrace },
					new TabItem { Header = "Log", Content = listBoxLog },
				},
			};
			linkLabelShowDetails.Click += (s, e) =>
			{
				tabControlDetails.IsVisible = !tabControlDetails.IsVisible;
				linkLabelShowDetails.Content = tabControlDetails.IsVisible ? "Hide details" : "Show details";
			};
			var buttonOK = new Button { Content = "OK", MinWidth = 75, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = true, IsCancel = true };
			buttonOK.Click += (s, e) => Close();

			var bottom = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
			DockPanel.SetDock(buttonOK, Dock.Right);
			bottom.Children.Add(buttonOK);
			bottom.Children.Add(linkLabelShowDetails);

			Content = new StackPanel { Margin = new Thickness(12), Spacing = 6, Children = { textBoxCaption, textBoxExceptionMessage, tabControlDetails, bottom } };
		}

		/// <summary>
		/// Shows the report modally over the active window (ShowDialog of the WinForms form).
		/// </summary>
		public static Task Show(string taskName, Exception exception, MemoryLogger logger)
		{
			var dialog = new ExceptionReportDialog(taskName, exception, logger);
			return dialog.ShowModalAsync(null);
		}
	}
}
