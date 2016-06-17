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
	public partial class PreprocessViewControl : ContentViewControl
	{
		public PreprocessViewControl()
		{
			InitializeComponent();
		}

		Control contentPanel;

		public override Control Content
		{
			get { return contentPanel; }
			set
			{
				if (contentPanel != value)
				{
					if (contentPanel != null)
						Controls.Remove(contentPanel);
					contentPanel = value;
					if (contentPanel != null)
						Controls.Add(contentPanel);
				}
			}
		}
	}
}
