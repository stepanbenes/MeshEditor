using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MeshEditor.Common.Logging;
using System.Threading.Tasks;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// Trida zobrazujici prubeh nacitani/ukladani site ze/do souboru
	/// </summary>
	public partial class ProgressViewForm : Form
	{
		public string Caption
		{
			get { return caption; }
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
			get { return operationName; }
			set
			{
				if (operationName != value)
				{
					operationName = value;
					updateOperationNameLabel();
				}
			}
		}

		string caption, operationName;
		int percentIndicator;
		bool quitRequested;

		readonly IMemoryLogger logger;

		/// <summary>
		/// udalost informujici o tom ze uzivatel stiskl tlacitko Cancel - zrusil prubeh procesu
		/// </summary>
		public event EventHandler Cancel;

		/// <summary>
		/// Konstruktor formulare
		/// </summary>
		/// <param name="caption">titulek, ktery se zobrazi v zahlavi</param>
		public ProgressViewForm(string caption, bool enableCancellation, IMemoryLogger logger = null)
		{
			InitializeComponent();
			this.logger = logger;
			this.Caption = caption;
			this.buttonCancel.Enabled = enableCancellation;
			progressBar.Minimum = 0;
			progressBar.Maximum = 100;
		}

		protected override void OnShown(EventArgs e)
		{
			if (logger != null)
			{
				logger.LogRecordReported += logRecordReported_handler; // subscribe to log
			}
		}

		private void logRecordReported_handler(object sender, LogRecordEventArgs args)
		{
			// send message to UI thread's message loop
			BeginInvoke((Action)(() =>
			{
				if (!listBoxLog.Visible)
				{
					this.Width = 600; // change width and height of the form to show listBoxLog
					this.Height = 500;
					this.listBoxLog.Visible = true; // show listBoxLog
				}

				// NOTE: don't report missed items, ignore history

				listBoxLog.Items.Add(createLogMessage(args.LogRecord));
				listBoxLog.TopIndex = listBoxLog.Items.Count - 1; // scroll to bottom
			}));

			string createLogMessage(LogRecord logRecord) => (logRecord.Type == RecordType.OperationProgress) ? logRecord.Content : $"[{logRecord.Type}] {logRecord.Content}";
		}

		protected override void OnClosed(EventArgs e)
		{
			if (logger != null)
			{
				logger.LogRecordReported -= logRecordReported_handler; // unsubscribe from log
			}
		}

		/// <summary>
		/// updatuje titulek v zahlavi
		/// </summary>
		private void updateCaption()
		{
			this.Text = Caption;
			if (percentIndicator > 0)
				this.Text += " (" + percentIndicator + "%)";
		}

		private void updateOperationNameLabel()
		{
			labelOperationName.Text = operationName;
		}

		/// <summary>
		/// nastavi pocet procent vykonaneho procesu
		/// </summary>
		/// <param name="percent">pocet procent</param>
		public void SetProgressState(int percent)
		{
			percentIndicator = percent;
			if (percent < 0)
			{
				progressBar.Style = ProgressBarStyle.Marquee;
			}
			else
			{
				progressBar.Style = ProgressBarStyle.Continuous;
				progressBar.Value = percent;
			}
			updateCaption();
		}

		/// <summary>
		/// reakce na stisk tlacitka Cancel - vyvola se udalost Cancel
		/// </summary>
		private void buttonCancel_Click(object sender, EventArgs e)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			buttonCancel.Enabled = false;
		}

		public void Quit()
		{
			quitRequested = true;
			this.Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!quitRequested)
			{
				e.Cancel = true;
			}
		}
	}
}
