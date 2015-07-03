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
	/// dialogove okne slouzici informovani uzivatele o nejake skutecnosti.
	/// navic obsahuje zaskrtavaci policko, ktere specifikuje, zda ma byt toto okno zobrazeno i v budoucnosti.
	/// </summary>
	public partial class CheckMessageBox : Form
	{
		private bool isChecked;

		public bool IsChecked
		{
			get { return isChecked; }
			set { checkBox1.Checked = isChecked = value; }
		}

		public CheckMessageBox(string message, string caption)
		{
			InitializeComponent();
			isChecked = false;
			this.Text = caption;
			label1.Text = message;
			checkBox1.Text = "Don't show this again";
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			isChecked = checkBox1.Checked;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		
	}
}
