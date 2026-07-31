using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MeshEditor.WinUI
{
	public partial class CheckedInputValueForm : InputValueForm
	{

		public CheckedInputValueForm()
		    : this("Input value", string.Empty, "is checked")
		{ }

		public CheckedInputValueForm(string caption, string infoText, string checkBoxText)
			: this(caption, infoText, SavedValue, checkBoxText)
		{ }

		public CheckedInputValueForm(string caption, string infoText, string boxValue, string checkBoxText)
			: base(caption, infoText, boxValue)
		{
			InitializeComponent();
			this.Text = caption;
			checkBox.Text = checkBoxText;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsChecked
		{
			get { return checkBox.Checked; }
			set { checkBox.Checked = value; }
		}

	}
}
