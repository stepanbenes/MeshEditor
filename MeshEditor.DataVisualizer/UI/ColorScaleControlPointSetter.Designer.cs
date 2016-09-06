namespace MeshEditor.DataVisualizer.UI
{
	partial class ColorScaleControlPointSetter
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
			this.checkBoxIsFixed = new System.Windows.Forms.CheckBox();
			this.textBoxValue = new System.Windows.Forms.TextBox();
			this.pictureBoxColor = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxColor)).BeginInit();
			this.SuspendLayout();
			// 
			// checkBoxIsFixed
			// 
			this.checkBoxIsFixed.AutoSize = true;
			this.checkBoxIsFixed.Location = new System.Drawing.Point(3, 6);
			this.checkBoxIsFixed.Name = "checkBoxIsFixed";
			this.checkBoxIsFixed.Size = new System.Drawing.Size(15, 14);
			this.checkBoxIsFixed.TabIndex = 0;
			this.checkBoxIsFixed.UseVisualStyleBackColor = true;
			this.checkBoxIsFixed.CheckedChanged += new System.EventHandler(this.checkBoxIsFixed_CheckedChanged);
			// 
			// textBoxValue
			// 
			this.textBoxValue.Location = new System.Drawing.Point(24, 3);
			this.textBoxValue.Name = "textBoxValue";
			this.textBoxValue.Size = new System.Drawing.Size(91, 20);
			this.textBoxValue.TabIndex = 1;
			this.textBoxValue.TextChanged += new System.EventHandler(this.textBoxValue_TextChanged);
			// 
			// pictureBoxColor
			// 
			this.pictureBoxColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBoxColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBoxColor.Location = new System.Drawing.Point(121, 3);
			this.pictureBoxColor.Name = "pictureBoxColor";
			this.pictureBoxColor.Size = new System.Drawing.Size(74, 20);
			this.pictureBoxColor.TabIndex = 2;
			this.pictureBoxColor.TabStop = false;
			this.pictureBoxColor.Click += new System.EventHandler(this.pictureBoxPropertyColor_Click);
			// 
			// ColorScaleControlPointSetter
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.pictureBoxColor);
			this.Controls.Add(this.textBoxValue);
			this.Controls.Add(this.checkBoxIsFixed);
			this.Name = "ColorScaleControlPointSetter";
			this.Size = new System.Drawing.Size(198, 27);
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxColor)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxIsFixed;
		private System.Windows.Forms.TextBox textBoxValue;
		private System.Windows.Forms.PictureBox pictureBoxColor;
	}
}
