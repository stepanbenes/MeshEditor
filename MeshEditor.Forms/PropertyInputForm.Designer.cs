namespace MeshEditor.WinUI
{
	partial class PropertyInputForm
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
			this.linkLabelEditPropertyComment = new System.Windows.Forms.LinkLabel();
			this.buttonOK = new System.Windows.Forms.Button();
			this.labelText = new System.Windows.Forms.Label();
			this.textBoxValue = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// linkLabelEditPropertyComment
			// 
			this.linkLabelEditPropertyComment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.linkLabelEditPropertyComment.AutoSize = true;
			this.linkLabelEditPropertyComment.Location = new System.Drawing.Point(8, 93);
			this.linkLabelEditPropertyComment.Name = "linkLabelEditPropertyComment";
			this.linkLabelEditPropertyComment.Size = new System.Drawing.Size(112, 13);
			this.linkLabelEditPropertyComment.TabIndex = 4;
			this.linkLabelEditPropertyComment.TabStop = true;
			this.linkLabelEditPropertyComment.Text = "Edit property comment";
			this.linkLabelEditPropertyComment.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelEditPropertyComment_LinkClicked);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(300, 68);
			this.buttonOK.Margin = new System.Windows.Forms.Padding(2);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(49, 23);
			this.buttonOK.TabIndex = 7;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// labelText
			// 
			this.labelText.AutoSize = true;
			this.labelText.Location = new System.Drawing.Point(8, 9);
			this.labelText.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelText.Name = "labelText";
			this.labelText.Size = new System.Drawing.Size(35, 13);
			this.labelText.TabIndex = 6;
			this.labelText.Text = "label1";
			// 
			// textBoxValue
			// 
			this.textBoxValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.textBoxValue.Location = new System.Drawing.Point(11, 68);
			this.textBoxValue.Margin = new System.Windows.Forms.Padding(2);
			this.textBoxValue.Name = "textBoxValue";
			this.textBoxValue.Size = new System.Drawing.Size(285, 23);
			this.textBoxValue.TabIndex = 5;
			this.textBoxValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxValue_KeyDown);
			// 
			// PropertyInputForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(360, 115);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.labelText);
			this.Controls.Add(this.textBoxValue);
			this.Controls.Add(this.linkLabelEditPropertyComment);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PropertyInputForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Property input";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.LinkLabel linkLabelEditPropertyComment;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label labelText;
		private System.Windows.Forms.TextBox textBoxValue;
	}
}