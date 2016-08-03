namespace MeshEditor.DataVisualizer.UI
{
	partial class ExceptionReportForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.textBoxExceptionMessage = new System.Windows.Forms.TextBox();
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			this.textBoxCaption = new System.Windows.Forms.TextBox();
			this.tabControlDetails = new System.Windows.Forms.TabControl();
			this.tabPageStackTrace = new System.Windows.Forms.TabPage();
			this.textBoxStackTrace = new System.Windows.Forms.TextBox();
			this.tabPageLog = new System.Windows.Forms.TabPage();
			this.listBoxLog = new System.Windows.Forms.ListBox();
			this.linkLabelShowDetails = new System.Windows.Forms.LinkLabel();
			this.buttonOK = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.tabControlDetails.SuspendLayout();
			this.tabPageStackTrace.SuspendLayout();
			this.tabPageLog.SuspendLayout();
			this.SuspendLayout();
			// 
			// textBoxExceptionMessage
			// 
			this.textBoxExceptionMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxExceptionMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxExceptionMessage.Location = new System.Drawing.Point(0, 19);
			this.textBoxExceptionMessage.Multiline = true;
			this.textBoxExceptionMessage.Name = "textBoxExceptionMessage";
			this.textBoxExceptionMessage.ReadOnly = true;
			this.textBoxExceptionMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxExceptionMessage.Size = new System.Drawing.Size(536, 40);
			this.textBoxExceptionMessage.TabIndex = 0;
			// 
			// splitContainer
			// 
			this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer.Location = new System.Drawing.Point(12, 12);
			this.splitContainer.Name = "splitContainer";
			this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer.Panel1
			// 
			this.splitContainer.Panel1.Controls.Add(this.textBoxCaption);
			this.splitContainer.Panel1.Controls.Add(this.textBoxExceptionMessage);
			// 
			// splitContainer.Panel2
			// 
			this.splitContainer.Panel2.Controls.Add(this.tabControlDetails);
			this.splitContainer.Size = new System.Drawing.Size(536, 265);
			this.splitContainer.SplitterDistance = 61;
			this.splitContainer.TabIndex = 2;
			// 
			// textBoxCaption
			// 
			this.textBoxCaption.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxCaption.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxCaption.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxCaption.ForeColor = System.Drawing.Color.Red;
			this.textBoxCaption.Location = new System.Drawing.Point(0, 0);
			this.textBoxCaption.Name = "textBoxCaption";
			this.textBoxCaption.ReadOnly = true;
			this.textBoxCaption.Size = new System.Drawing.Size(536, 13);
			this.textBoxCaption.TabIndex = 2;
			// 
			// tabControlDetails
			// 
			this.tabControlDetails.Controls.Add(this.tabPageStackTrace);
			this.tabControlDetails.Controls.Add(this.tabPageLog);
			this.tabControlDetails.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControlDetails.Location = new System.Drawing.Point(0, 0);
			this.tabControlDetails.Name = "tabControlDetails";
			this.tabControlDetails.SelectedIndex = 0;
			this.tabControlDetails.Size = new System.Drawing.Size(536, 200);
			this.tabControlDetails.TabIndex = 0;
			// 
			// tabPageStackTrace
			// 
			this.tabPageStackTrace.Controls.Add(this.textBoxStackTrace);
			this.tabPageStackTrace.Location = new System.Drawing.Point(4, 22);
			this.tabPageStackTrace.Name = "tabPageStackTrace";
			this.tabPageStackTrace.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageStackTrace.Size = new System.Drawing.Size(554, 174);
			this.tabPageStackTrace.TabIndex = 0;
			this.tabPageStackTrace.Text = "Stack trace";
			this.tabPageStackTrace.UseVisualStyleBackColor = true;
			// 
			// textBoxStackTrace
			// 
			this.textBoxStackTrace.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxStackTrace.Location = new System.Drawing.Point(3, 3);
			this.textBoxStackTrace.Multiline = true;
			this.textBoxStackTrace.Name = "textBoxStackTrace";
			this.textBoxStackTrace.ReadOnly = true;
			this.textBoxStackTrace.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxStackTrace.Size = new System.Drawing.Size(548, 168);
			this.textBoxStackTrace.TabIndex = 0;
			// 
			// tabPageLog
			// 
			this.tabPageLog.Controls.Add(this.listBoxLog);
			this.tabPageLog.Location = new System.Drawing.Point(4, 22);
			this.tabPageLog.Name = "tabPageLog";
			this.tabPageLog.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageLog.Size = new System.Drawing.Size(528, 174);
			this.tabPageLog.TabIndex = 1;
			this.tabPageLog.Text = "Log";
			this.tabPageLog.UseVisualStyleBackColor = true;
			// 
			// listBoxLog
			// 
			this.listBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxLog.FormattingEnabled = true;
			this.listBoxLog.HorizontalScrollbar = true;
			this.listBoxLog.Location = new System.Drawing.Point(3, 3);
			this.listBoxLog.Name = "listBoxLog";
			this.listBoxLog.Size = new System.Drawing.Size(522, 168);
			this.listBoxLog.TabIndex = 0;
			// 
			// linkLabelShowDetails
			// 
			this.linkLabelShowDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.linkLabelShowDetails.AutoSize = true;
			this.linkLabelShowDetails.Location = new System.Drawing.Point(12, 288);
			this.linkLabelShowDetails.Name = "linkLabelShowDetails";
			this.linkLabelShowDetails.Size = new System.Drawing.Size(67, 13);
			this.linkLabelShowDetails.TabIndex = 1;
			this.linkLabelShowDetails.TabStop = true;
			this.linkLabelShowDetails.Text = "Show details";
			this.linkLabelShowDetails.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelShowDetails_LinkClicked);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(473, 283);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 3;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// ExceptionReportForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonOK;
			this.ClientSize = new System.Drawing.Size(560, 318);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.linkLabelShowDetails);
			this.Controls.Add(this.splitContainer);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExceptionReportForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Error report";
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel1.PerformLayout();
			this.splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
			this.splitContainer.ResumeLayout(false);
			this.tabControlDetails.ResumeLayout(false);
			this.tabPageStackTrace.ResumeLayout(false);
			this.tabPageStackTrace.PerformLayout();
			this.tabPageLog.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBoxExceptionMessage;
		private System.Windows.Forms.SplitContainer splitContainer;
		private System.Windows.Forms.TabControl tabControlDetails;
		private System.Windows.Forms.TabPage tabPageStackTrace;
		private System.Windows.Forms.TabPage tabPageLog;
		private System.Windows.Forms.TextBox textBoxStackTrace;
		private System.Windows.Forms.ListBox listBoxLog;
		private System.Windows.Forms.LinkLabel linkLabelShowDetails;
		private System.Windows.Forms.TextBox textBoxCaption;
		private System.Windows.Forms.Button buttonOK;
	}
}