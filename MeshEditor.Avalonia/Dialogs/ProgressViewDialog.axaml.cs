using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MeshEditor.Common.Logging;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Progress of a long operation with optional cancellation and log (port of MeshEditor.WinUI.ProgressViewForm).
	/// </summary>
	public partial class ProgressViewDialog : Window
	{
		private string caption, operationName;
		private int percentIndicator;
		private bool quitRequested;
		private readonly IMemoryLogger logger;

		public event EventHandler Cancel;

		public ProgressViewDialog()
			: this("Operation in progress ...", false)
		{ }

		public ProgressViewDialog(string caption, bool enableCancellation, IMemoryLogger logger = null)
		{
			InitializeComponent();
			this.logger = logger;
			this.Caption = caption;
			this.buttonCancel.IsEnabled = enableCancellation;
		}

		public string Caption
		{
			get => caption;
			set
			{
				if (caption != value)
				{
					caption = value;
					updateCaption();
				}
			}
		}

		public string OperationName
		{
			get => operationName;
			set
			{
				if (operationName != value)
				{
					operationName = value;
					labelOperationName.Text = operationName;
				}
			}
		}

		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
			if (logger != null)
				logger.LogRecordReported += logRecordReported_handler; // subscribe to log
		}

		private void logRecordReported_handler(object sender, LogRecordEventArgs args)
		{
			Dispatcher.UIThread.Post(() =>
			{
				if (!listBoxLog.IsVisible)
				{
					SizeToContent = SizeToContent.Manual;
					Width = 600; // enlarge the window to show the log
					Height = 500;
					listBoxLog.IsVisible = true;
				}
				listBoxLog.Items.Add(createLogMessage(args.LogRecord));
				listBoxLog.ScrollIntoView(listBoxLog.Items.Count - 1); // scroll to bottom
			});

			static string createLogMessage(LogRecord logRecord) => (logRecord.Type == RecordType.OperationProgress) ? logRecord.Content : $"[{logRecord.Type}] {logRecord.Content}";
		}

		protected override void OnClosed(EventArgs e)
		{
			if (logger != null)
				logger.LogRecordReported -= logRecordReported_handler; // unsubscribe from log
			base.OnClosed(e);
		}

		private void updateCaption()
		{
			string text = Caption;
			if (percentIndicator > 0)
				text += " (" + percentIndicator + "%)";
			Title = text;
		}

		public void SetProgressState(int percent)
		{
			percentIndicator = percent;
			if (percent < 0)
			{
				progressBar.IsIndeterminate = true;
			}
			else
			{
				progressBar.IsIndeterminate = false;
				progressBar.Value = Math.Min(100, percent);
			}
			updateCaption();
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			buttonCancel.IsEnabled = false;
		}

		/// <summary>
		/// Closes the window (the user can't close it, like the WinForms form without control box).
		/// </summary>
		public void Quit()
		{
			quitRequested = true;
			Close();
		}

		protected override void OnClosing(WindowClosingEventArgs e)
		{
			if (!quitRequested)
			{
				// the WinForms form has no close button; Avalonia can't hide it on all platforms, so it acts as Cancel
				e.Cancel = true;
				if (buttonCancel.IsEnabled)
					buttonCancel_Click(this, null);
			}
			base.OnClosing(e);
		}
	}
}
