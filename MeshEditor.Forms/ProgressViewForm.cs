using System;
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
			this.Caption = caption;
			this.buttonCancel.Enabled = enableCancellation;
			progressBar.Minimum = 0;
			progressBar.Maximum = 100;

			if (logger != null)
			{
				// change width and height of the form to show listBoxLog
				this.Width = 600;
				this.Height = 500;
				// show listBoxLog
				this.listBoxLog.Visible = true;
				// subscribe to log
				logger.LogRecordReported += (sender, args) =>
				{
					// must be run on UI thread
					BeginInvoke((Action)(() =>
					{
						string message = (args.LogRecord.Type == RecordType.OperationProgress) ?
											args.LogRecord.Content :
											$"[{args.LogRecord.Type}] {args.LogRecord.Content}";
						listBoxLog.Items.Add(message);
						listBoxLog.TopIndex = listBoxLog.Items.Count - 1; // scroll to bottom
					}));
				};
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
