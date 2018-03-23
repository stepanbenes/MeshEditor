namespace MeshEditor.DataVisualizer.UI
{
	partial class VectorFieldVisualizationSettingsForm
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
			this.buttonApply = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.labelArrowLengthFactor = new System.Windows.Forms.Label();
			this.trackBarVectorLengthFactor = new System.Windows.Forms.TrackBar();
			this.checkBoxInvertVectorArrows = new System.Windows.Forms.CheckBox();
			this.textBoxVectorLengthFactor = new System.Windows.Forms.TextBox();
			this.checkBoxIsArrowLengthFixed = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.trackBarVectorLengthFactor)).BeginInit();
			this.SuspendLayout();
			// 
			// buttonApply
			// 
			this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonApply.Location = new System.Drawing.Point(343, 181);
			this.buttonApply.Name = "buttonApply";
			this.buttonApply.Size = new System.Drawing.Size(100, 28);
			this.buttonApply.TabIndex = 14;
			this.buttonApply.Text = "Apply";
			this.buttonApply.UseVisualStyleBackColor = true;
			this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(236, 181);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(4);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(100, 28);
			this.buttonCancel.TabIndex = 13;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(128, 181);
			this.buttonOK.Margin = new System.Windows.Forms.Padding(4);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(100, 28);
			this.buttonOK.TabIndex = 12;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// labelArrowLengthFactor
			// 
			this.labelArrowLengthFactor.AutoSize = true;
			this.labelArrowLengthFactor.Location = new System.Drawing.Point(24, 76);
			this.labelArrowLengthFactor.Name = "labelArrowLengthFactor";
			this.labelArrowLengthFactor.Size = new System.Drawing.Size(131, 17);
			this.labelArrowLengthFactor.TabIndex = 17;
			this.labelArrowLengthFactor.Text = "Arrow length factor:";
			// 
			// trackBarVectorLengthFactor
			// 
			this.trackBarVectorLengthFactor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBarVectorLengthFactor.LargeChange = 10;
			this.trackBarVectorLengthFactor.Location = new System.Drawing.Point(12, 103);
			this.trackBarVectorLengthFactor.Maximum = 200;
			this.trackBarVectorLengthFactor.Minimum = 1;
			this.trackBarVectorLengthFactor.Name = "trackBarVectorLengthFactor";
			this.trackBarVectorLengthFactor.Size = new System.Drawing.Size(431, 56);
			this.trackBarVectorLengthFactor.SmallChange = 5;
			this.trackBarVectorLengthFactor.TabIndex = 16;
			this.trackBarVectorLengthFactor.Value = 100;
			this.trackBarVectorLengthFactor.ValueChanged += new System.EventHandler(this.trackBarVectorLengthFactor_ValueChanged);
			// 
			// checkBoxInvertVectorArrows
			// 
			this.checkBoxInvertVectorArrows.AutoSize = true;
			this.checkBoxInvertVectorArrows.Location = new System.Drawing.Point(12, 12);
			this.checkBoxInvertVectorArrows.Name = "checkBoxInvertVectorArrows";
			this.checkBoxInvertVectorArrows.Size = new System.Drawing.Size(154, 21);
			this.checkBoxInvertVectorArrows.TabIndex = 15;
			this.checkBoxInvertVectorArrows.Text = "Invert vector arrows";
			this.checkBoxInvertVectorArrows.UseVisualStyleBackColor = true;
			this.checkBoxInvertVectorArrows.CheckedChanged += new System.EventHandler(this.checkBoxInvertVectorArrows_CheckedChanged);
			// 
			// textBoxVectorLengthFactor
			// 
			this.textBoxVectorLengthFactor.Location = new System.Drawing.Point(161, 73);
			this.textBoxVectorLengthFactor.Name = "textBoxVectorLengthFactor";
			this.textBoxVectorLengthFactor.Size = new System.Drawing.Size(100, 22);
			this.textBoxVectorLengthFactor.TabIndex = 18;
			this.textBoxVectorLengthFactor.TextChanged += new System.EventHandler(this.textBoxVectorLengthFactor_TextChanged);
			// 
			// checkBoxIsArrowLengthFixed
			// 
			this.checkBoxIsArrowLengthFixed.AutoSize = true;
			this.checkBoxIsArrowLengthFixed.Location = new System.Drawing.Point(12, 39);
			this.checkBoxIsArrowLengthFixed.Name = "checkBoxIsArrowLengthFixed";
			this.checkBoxIsArrowLengthFixed.Size = new System.Drawing.Size(129, 21);
			this.checkBoxIsArrowLengthFixed.TabIndex = 19;
			this.checkBoxIsArrowLengthFixed.Text = "Fix arrow length";
			this.checkBoxIsArrowLengthFixed.UseVisualStyleBackColor = true;
			this.checkBoxIsArrowLengthFixed.CheckedChanged += new System.EventHandler(this.checkBoxIsArrowLengthFixed_CheckedChanged);
			// 
			// VectorFieldVisualizationSettingsForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(456, 222);
			this.Controls.Add(this.checkBoxIsArrowLengthFixed);
			this.Controls.Add(this.textBoxVectorLengthFactor);
			this.Controls.Add(this.labelArrowLengthFactor);
			this.Controls.Add(this.trackBarVectorLengthFactor);
			this.Controls.Add(this.checkBoxInvertVectorArrows);
			this.Controls.Add(this.buttonApply);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "VectorFieldVisualizationSettingsForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Vector field visualization settings";
			((System.ComponentModel.ISupportInitialize)(this.trackBarVectorLengthFactor)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonApply;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label labelArrowLengthFactor;
		private System.Windows.Forms.TrackBar trackBarVectorLengthFactor;
		private System.Windows.Forms.CheckBox checkBoxInvertVectorArrows;
		private System.Windows.Forms.TextBox textBoxVectorLengthFactor;
		private System.Windows.Forms.CheckBox checkBoxIsArrowLengthFixed;
	}
}