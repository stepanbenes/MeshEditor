namespace MeshEditor.WinUI
{
	partial class PropertyColorControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.labelPropertyNumber = new System.Windows.Forms.Label();
			this.pictureBoxPropertyColor = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxPropertyColor)).BeginInit();
			this.SuspendLayout();
			// 
			// labelPropertyNumber
			// 
			this.labelPropertyNumber.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.labelPropertyNumber.AutoSize = true;
			this.labelPropertyNumber.Location = new System.Drawing.Point(4, 4);
			this.labelPropertyNumber.Name = "labelPropertyNumber";
			this.labelPropertyNumber.Size = new System.Drawing.Size(84, 13);
			this.labelPropertyNumber.TabIndex = 0;
			this.labelPropertyNumber.Text = "Property number";
			// 
			// pictureBoxPropertyColor
			// 
			this.pictureBoxPropertyColor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBoxPropertyColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBoxPropertyColor.Location = new System.Drawing.Point(98, 2);
			this.pictureBoxPropertyColor.Name = "pictureBoxPropertyColor";
			this.pictureBoxPropertyColor.Size = new System.Drawing.Size(120, 20);
			this.pictureBoxPropertyColor.TabIndex = 1;
			this.pictureBoxPropertyColor.TabStop = false;
			this.pictureBoxPropertyColor.Click += new System.EventHandler(this.pictureBoxPropertyColor_Click);
			// 
			// PropertyColorControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.Controls.Add(this.pictureBoxPropertyColor);
			this.Controls.Add(this.labelPropertyNumber);
			this.MinimumSize = new System.Drawing.Size(200, 20);
			this.Name = "PropertyColorControl";
			this.Size = new System.Drawing.Size(220, 25);
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxPropertyColor)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelPropertyNumber;
		private System.Windows.Forms.PictureBox pictureBoxPropertyColor;
	}
}
