using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.DataVisualizer.Services;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class ExceptionReportForm : Form
	{
		public ExceptionReportForm()
		{
			InitializeComponent();
		}

		internal ExceptionReportForm(string taskName, Exception exception, MemoryLogger logger)
			: this()
		{
			Debug.Assert(exception != null);
			Debug.Assert(logger != null);

			textBoxCaption.Text = "Error in processing task: " + taskName;
			textBoxExceptionMessage.Text = exception.Message;
			textBoxStackTrace.Text = exception.StackTrace;
			listBoxLog.Items.AddRange(logger.GetRecordHistory().Cast<object>().ToArray());

			linkLabelShowDetails_LinkClicked(null, null); // collapse panel 2
		}

		private void linkLabelShowDetails_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (splitContainer.Panel2Collapsed)
			{
				splitContainer.Panel2Collapsed = false;
				linkLabelShowDetails.Text = "Hide details";
				this.Height += 200;
			}
			else
			{
				this.Height -= splitContainer.Panel2.Height;
				splitContainer.Panel2Collapsed = true;
				linkLabelShowDetails.Text = "Show details";
			}
		}
	}
}
