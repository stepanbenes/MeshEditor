namespace MeshEditor.WinUI
{
	partial class ProgressViewForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgressViewForm));
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.labelOperationName = new System.Windows.Forms.Label();
			this.listBoxLog = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// progressBar
			// 
			this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar.Location = new System.Drawing.Point(12, 34);
			this.progressBar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.progressBar.MarqueeAnimationSpeed = 30;
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(407, 23);
			this.progressBar.Step = 1;
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar.TabIndex = 0;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(427, 31);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 31);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// labelOperationName
			// 
			this.labelOperationName.AutoSize = true;
			this.labelOperationName.Location = new System.Drawing.Point(8, 11);
			this.labelOperationName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelOperationName.Name = "labelOperationName";
			this.labelOperationName.Size = new System.Drawing.Size(0, 17);
			this.labelOperationName.TabIndex = 2;
			// 
			// listBoxLog
			// 
			this.listBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listBoxLog.FormattingEnabled = true;
			this.listBoxLog.HorizontalScrollbar = true;
			this.listBoxLog.ItemHeight = 16;
			this.listBoxLog.Location = new System.Drawing.Point(12, 78);
			this.listBoxLog.Name = "listBoxLog";
			this.listBoxLog.Size = new System.Drawing.Size(490, 4);
			this.listBoxLog.TabIndex = 3;
			this.listBoxLog.Visible = false;
			// 
			// ProgressViewForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(515, 76);
			this.ControlBox = false;
			this.Controls.Add(this.listBoxLog);
			this.Controls.Add(this.labelOperationName);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.progressBar);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.MinimumSize = new System.Drawing.Size(299, 123);
			this.Name = "ProgressViewForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Operation in progress ...";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Label labelOperationName;
		private System.Windows.Forms.ListBox listBoxLog;
	}
}