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
	public partial class PostprocessViewControl : ContentViewControl
	{
		public PostprocessViewControl()
		{
			InitializeComponent();
			splitContainer1.FixedPanel = FixedPanel.Panel1;
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
						splitContainer1.Panel2.Controls.Remove(contentPanel);
					contentPanel = value;
					if (contentPanel != null)
						splitContainer1.Panel2.Controls.Add(contentPanel);
				}
			}
		}

		public int SplitterDistance
		{
			get { return splitContainer1.SplitterDistance; }
			set { splitContainer1.SplitterDistance = value; }
		}
	}
}
