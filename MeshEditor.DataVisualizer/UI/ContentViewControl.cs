using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeshEditor.WinUI
{
	public partial class ContentViewControl : UserControl
	{
		public ContentViewControl()
		{
			InitializeComponent();
		}

		public virtual Control Content
		{
			get { return null; }
			set { }
		}
	}
}
