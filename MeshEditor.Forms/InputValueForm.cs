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
	/// dialog pro zadani vstupni hodnoty od uzivatele
	/// </summary>
	public partial class InputValueForm : Form
	{
		public event CancelEventHandler InputValueValidating;

		public static string SavedValue = "0";

		public InputValueForm()
		    : this("Input value", string.Empty)
		{ }

		public InputValueForm(string caption, string infoText)
			: this(caption, infoText, SavedValue)
		{ }

		public InputValueForm(string caption, string infoText, string boxValue)
		{
			InitializeComponent();

			//this.KeyDown += new KeyEventHandler(InputValueForm_KeyDown);
			
			this.Text = caption;
			initializeText(infoText);
			textBoxValue.Text = boxValue;
			textBoxValue.CausesValidation = false;
			//textBoxValue.Validating += new CancelEventHandler(textBoxValue_Validating);
			textBoxValue.SelectAll();

			this.ClientSize = new Size((labelText.Width + 40 > 360) ? labelText.Width + 40 : 360, labelText.Height + 80);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			focusTextbox();
		}

		private void focusTextbox()
		{
			textBoxValue.SelectAll();
			textBoxValue.Focus();
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
			if (rows.Length > 1)
			{
				for (int i = 1; i < rows.Length; i++)
				{
					builder.AppendLine();
					builder.Append(rows[i].Trim());
				}
			}
			labelText.Text = builder.ToString();
		}

		public string InputValue
		{
			get { return textBoxValue.Text; }
		}

		protected bool validateInput()
		{
			CancelEventArgs args = new CancelEventArgs();
			if (InputValueValidating != null)
				InputValueValidating(this, args);
			this.Focus();
			if (!args.Cancel)
			{
				SavedValue = textBoxValue.Text;
				return true;
			}
			return false;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (validateInput())
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
			else
				focusTextbox();
		}

		private void textBoxValue_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				this.DialogResult = DialogResult.Cancel;
				this.Close();
			}
			else if (e.KeyCode == Keys.Return)
			{
				if (validateInput())
				{
					this.DialogResult = DialogResult.OK;
					this.Close();
				}
				else
					focusTextbox();
			}
		}

	}
}
