using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

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
		public ProgressViewForm(string caption, bool enableCancellation)
		{
			InitializeComponent();
			this.Caption = caption;
			this.buttonCancel.Enabled = enableCancellation;
			progressBar.Minimum = 0;
			progressBar.Maximum = 100;
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
			if (Cancel != null)
				Cancel(this, EventArgs.Empty);
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
