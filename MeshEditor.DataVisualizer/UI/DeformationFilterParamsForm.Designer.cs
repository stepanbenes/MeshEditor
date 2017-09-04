namespace MeshEditor.DataVisualizer.UI
{
	partial class DeformationFilterParamsForm
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
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.comboBoxDeformationField = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxLayerName = new System.Windows.Forms.TextBox();
			this.trackBarScale = new System.Windows.Forms.TrackBar();
			this.labelScale = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.trackBarScale)).BeginInit();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(344, 248);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(100, 28);
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(450, 248);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(100, 28);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// comboBoxDeformationField
			// 
			this.comboBoxDeformationField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDeformationField.FormattingEnabled = true;
			this.comboBoxDeformationField.Location = new System.Drawing.Point(11, 29);
			this.comboBoxDeformationField.Name = "comboBoxDeformationField";
			this.comboBoxDeformationField.Size = new System.Drawing.Size(249, 24);
			this.comboBoxDeformationField.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(234, 17);
			this.label1.TabIndex = 3;
			this.label1.Text = "Field with displacements information";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 148);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(159, 17);
			this.label2.TabIndex = 4;
			this.label2.Text = "Deformation layer name";
			// 
			// textBoxLayerName
			// 
			this.textBoxLayerName.Location = new System.Drawing.Point(11, 168);
			this.textBoxLayerName.Name = "textBoxLayerName";
			this.textBoxLayerName.Size = new System.Drawing.Size(249, 22);
			this.textBoxLayerName.TabIndex = 5;
			this.textBoxLayerName.Text = "deformation";
			// 
			// trackBarScale
			// 
			this.trackBarScale.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBarScale.LargeChange = 10;
			this.trackBarScale.Location = new System.Drawing.Point(11, 88);
			this.trackBarScale.Margin = new System.Windows.Forms.Padding(4);
			this.trackBarScale.Maximum = 100;
			this.trackBarScale.Name = "trackBarScale";
			this.trackBarScale.Size = new System.Drawing.Size(537, 56);
			this.trackBarScale.TabIndex = 6;
			this.trackBarScale.Value = 10;
			this.trackBarScale.ValueChanged += new System.EventHandler(this.trackBarScale_ValueChanged);
			// 
			// labelScale
			// 
			this.labelScale.AutoSize = true;
			this.labelScale.Location = new System.Drawing.Point(8, 67);
			this.labelScale.Name = "labelScale";
			this.labelScale.Size = new System.Drawing.Size(43, 17);
			this.labelScale.TabIndex = 7;
			this.labelScale.Text = "Scale";
			// 
			// DeformationFilterParamsForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(562, 288);
			this.Controls.Add(this.labelScale);
			this.Controls.Add(this.trackBarScale);
			this.Controls.Add(this.textBoxLayerName);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBoxDeformationField);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(272, 335);
			this.Name = "DeformationFilterParamsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Deformation filter parameters";
			((System.ComponentModel.ISupportInitialize)(this.trackBarScale)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.ComboBox comboBoxDeformationField;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxLayerName;
		private System.Windows.Forms.TrackBar trackBarScale;
		private System.Windows.Forms.Label labelScale;
	}
}