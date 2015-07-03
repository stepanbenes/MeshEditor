using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
		private readonly string caption;

		/// <summary>
		/// udalost informujici o tom ze uzivatel stiskl tlacitko Cancel - zrusil prubeh procesu
		/// </summary>
		public event EventHandler Cancel;

		/// <summary>
		/// Konstruktor formulare
		/// </summary>
		/// <param name="caption">titulek, ktery se zobrazi v zahlavi</param>
		public ProgressViewForm(string caption)
		{
			InitializeComponent();
			this.caption = caption;
			this.Text = this.caption;
			progressBar.Minimum = 0;
			progressBar.Maximum = 100;
		}

		/// <summary>
		/// updatuje titulek v zahlavi
		/// </summary>
		private void updateCaption(int percent)
		{
			this.Text = caption + " (" + percent + "%)";
		}

		/// <summary>
		/// nastavi pocet procent vykonaneho procesu
		/// </summary>
		/// <param name="percent">pocet procent</param>
		public void SetProgressState(int percent)
		{
			progressBar.Value = percent;
			updateCaption(percent);
		}

		/// <summary>
		/// reakce na stisk tlacitka Cancel - vyvola se udalost Cancel
		/// </summary>
		private void buttonCancel_Click(object sender, EventArgs e)
		{
			if (Cancel != null)
				Cancel(this, EventArgs.Empty);
		}

	}
}
