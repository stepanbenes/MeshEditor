namespace MeshEditor.WinUI
{
	partial class RemoteSolutionsForm
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
			this.listBoxSolutions = new System.Windows.Forms.ListBox();
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOpenInBrowser = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// listBoxSolutions
			// 
			this.listBoxSolutions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listBoxSolutions.FormattingEnabled = true;
			this.listBoxSolutions.Location = new System.Drawing.Point(12, 12);
			this.listBoxSolutions.Name = "listBoxSolutions";
			this.listBoxSolutions.Size = new System.Drawing.Size(391, 251);
			this.listBoxSolutions.TabIndex = 0;
			this.listBoxSolutions.SelectedIndexChanged += new System.EventHandler(this.listBoxSolutions_SelectedIndexChanged);
			// 
			// buttonOk
			// 
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(247, 283);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 1;
			this.buttonOk.Text = "OK";
			this.buttonOk.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(328, 283);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 2;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOpenInBrowser
			// 
			this.buttonOpenInBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonOpenInBrowser.Location = new System.Drawing.Point(12, 283);
			this.buttonOpenInBrowser.Name = "buttonOpenInBrowser";
			this.buttonOpenInBrowser.Size = new System.Drawing.Size(113, 23);
			this.buttonOpenInBrowser.TabIndex = 3;
			this.buttonOpenInBrowser.Text = "Open in browser";
			this.buttonOpenInBrowser.UseVisualStyleBackColor = true;
			this.buttonOpenInBrowser.Click += new System.EventHandler(this.buttonOpenInBrowser_Click);
			// 
			// RemoteSolutionsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(415, 318);
			this.Controls.Add(this.buttonOpenInBrowser);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.listBoxSolutions);
			this.Name = "RemoteSolutionsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Remote solutions";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxSolutions;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOpenInBrowser;
	}
}