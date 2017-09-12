namespace MeshEditor.DataVisualizer.UI
{
	partial class SliceFilterParamsForm
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
			this.buttonSelectTwoPoints = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonSelectThreePoints = new System.Windows.Forms.Button();
			this.textBoxLayerName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.labelSuggestion = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// buttonSelectTwoPoints
			// 
			this.buttonSelectTwoPoints.Location = new System.Drawing.Point(12, 12);
			this.buttonSelectTwoPoints.Name = "buttonSelectTwoPoints";
			this.buttonSelectTwoPoints.Size = new System.Drawing.Size(267, 35);
			this.buttonSelectTwoPoints.TabIndex = 0;
			this.buttonSelectTwoPoints.Text = "Define plane using two points";
			this.buttonSelectTwoPoints.UseVisualStyleBackColor = true;
			this.buttonSelectTwoPoints.Click += new System.EventHandler(this.buttonSelectTwoPoints_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(241, 192);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(100, 28);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(135, 192);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(100, 28);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonSelectThreePoints
			// 
			this.buttonSelectThreePoints.Location = new System.Drawing.Point(12, 53);
			this.buttonSelectThreePoints.Name = "buttonSelectThreePoints";
			this.buttonSelectThreePoints.Size = new System.Drawing.Size(267, 35);
			this.buttonSelectThreePoints.TabIndex = 4;
			this.buttonSelectThreePoints.Text = "Define plane using three points";
			this.buttonSelectThreePoints.UseVisualStyleBackColor = true;
			this.buttonSelectThreePoints.Click += new System.EventHandler(this.buttonSelectThreePoints_Click);
			// 
			// textBoxLayerName
			// 
			this.textBoxLayerName.Location = new System.Drawing.Point(12, 143);
			this.textBoxLayerName.Name = "textBoxLayerName";
			this.textBoxLayerName.Size = new System.Drawing.Size(267, 22);
			this.textBoxLayerName.TabIndex = 7;
			this.textBoxLayerName.Text = "slice";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 123);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(112, 17);
			this.label2.TabIndex = 6;
			this.label2.Text = "Slice layer name";
			// 
			// labelSuggestion
			// 
			this.labelSuggestion.AutoSize = true;
			this.labelSuggestion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
			this.labelSuggestion.Location = new System.Drawing.Point(12, 91);
			this.labelSuggestion.Name = "labelSuggestion";
			this.labelSuggestion.Size = new System.Drawing.Size(321, 17);
			this.labelSuggestion.TabIndex = 8;
			this.labelSuggestion.Text = "Select plane definition points by clicking on nodes";
			this.labelSuggestion.Visible = false;
			// 
			// SliceFilterParamsForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(353, 232);
			this.Controls.Add(this.labelSuggestion);
			this.Controls.Add(this.textBoxLayerName);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.buttonSelectThreePoints);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonSelectTwoPoints);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SliceFilterParamsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Slice filter parameters";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonSelectTwoPoints;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonSelectThreePoints;
		private System.Windows.Forms.TextBox textBoxLayerName;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label labelSuggestion;
	}
}