namespace MeshEditor.WinUI
{
	partial class CheckedInputValueForm
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
			this.checkBox = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// checkBox
			// 
			this.checkBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBox.AutoSize = true;
			this.checkBox.Location = new System.Drawing.Point(9, 37);
			this.checkBox.Name = "checkBox";
			this.checkBox.Size = new System.Drawing.Size(78, 17);
			this.checkBox.TabIndex = 4;
			this.checkBox.Text = "is checked";
			this.checkBox.UseVisualStyleBackColor = true;
			// 
			// CheckedInputValueForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(360, 98);
			this.Controls.Add(this.checkBox);
			this.Name = "CheckedInputValueForm";
			this.Text = "CheckedInputValueForm";
			this.Controls.SetChildIndex(this.checkBox, 0);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBox;
	}
}