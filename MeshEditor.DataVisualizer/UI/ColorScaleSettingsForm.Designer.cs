namespace MeshEditor.DataVisualizer.UI
{
	partial class ColorScaleSettingsForm
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
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxColorScaleType = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.controlPointsPanel = new System.Windows.Forms.Panel();
			this.comboBoxNumberOfSubIntervals = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.checkBoxShowIsoAreas = new System.Windows.Forms.CheckBox();
			this.buttonApply = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(66, 370);
			this.buttonOK.Margin = new System.Windows.Forms.Padding(4);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(100, 28);
			this.buttonOK.TabIndex = 1;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(174, 370);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(4);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(100, 28);
			this.buttonCancel.TabIndex = 2;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 71);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(167, 17);
			this.label1.TabIndex = 3;
			this.label1.Text = "Color scale control points";
			// 
			// comboBoxColorScaleType
			// 
			this.comboBoxColorScaleType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxColorScaleType.FormattingEnabled = true;
			this.comboBoxColorScaleType.Location = new System.Drawing.Point(20, 31);
			this.comboBoxColorScaleType.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxColorScaleType.Name = "comboBoxColorScaleType";
			this.comboBoxColorScaleType.Size = new System.Drawing.Size(167, 24);
			this.comboBoxColorScaleType.TabIndex = 6;
			this.comboBoxColorScaleType.SelectedIndexChanged += new System.EventHandler(this.comboBoxColorScaleType_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 11);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(109, 17);
			this.label2.TabIndex = 5;
			this.label2.Text = "Color scale type";
			// 
			// controlPointsPanel
			// 
			this.controlPointsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.controlPointsPanel.AutoScroll = true;
			this.controlPointsPanel.Location = new System.Drawing.Point(20, 91);
			this.controlPointsPanel.Margin = new System.Windows.Forms.Padding(4);
			this.controlPointsPanel.Name = "controlPointsPanel";
			this.controlPointsPanel.Size = new System.Drawing.Size(361, 206);
			this.controlPointsPanel.TabIndex = 7;
			// 
			// comboBoxNumberOfSubIntervals
			// 
			this.comboBoxNumberOfSubIntervals.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxNumberOfSubIntervals.FormattingEnabled = true;
			this.comboBoxNumberOfSubIntervals.Location = new System.Drawing.Point(182, 327);
			this.comboBoxNumberOfSubIntervals.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxNumberOfSubIntervals.Name = "comboBoxNumberOfSubIntervals";
			this.comboBoxNumberOfSubIntervals.Size = new System.Drawing.Size(60, 24);
			this.comboBoxNumberOfSubIntervals.TabIndex = 10;
			this.comboBoxNumberOfSubIntervals.SelectedIndexChanged += new System.EventHandler(this.comboBoxNumberOfSubIntervals_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(15, 330);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(159, 17);
			this.label3.TabIndex = 9;
			this.label3.Text = "Number of sub-intervals";
			// 
			// checkBoxShowIsoAreas
			// 
			this.checkBoxShowIsoAreas.AutoSize = true;
			this.checkBoxShowIsoAreas.Location = new System.Drawing.Point(19, 305);
			this.checkBoxShowIsoAreas.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxShowIsoAreas.Name = "checkBoxShowIsoAreas";
			this.checkBoxShowIsoAreas.Size = new System.Drawing.Size(127, 21);
			this.checkBoxShowIsoAreas.TabIndex = 8;
			this.checkBoxShowIsoAreas.Text = "Show iso-areas";
			this.checkBoxShowIsoAreas.UseVisualStyleBackColor = true;
			this.checkBoxShowIsoAreas.CheckedChanged += new System.EventHandler(this.checkBoxShowIsoAreas_CheckedChanged);
			// 
			// buttonApply
			// 
			this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonApply.Location = new System.Drawing.Point(281, 370);
			this.buttonApply.Name = "buttonApply";
			this.buttonApply.Size = new System.Drawing.Size(100, 28);
			this.buttonApply.TabIndex = 11;
			this.buttonApply.Text = "Apply";
			this.buttonApply.UseVisualStyleBackColor = true;
			this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
			// 
			// ColorScaleSettingsForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(397, 412);
			this.Controls.Add(this.buttonApply);
			this.Controls.Add(this.comboBoxNumberOfSubIntervals);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.checkBoxShowIsoAreas);
			this.Controls.Add(this.controlPointsPanel);
			this.Controls.Add(this.comboBoxColorScaleType);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(374, 287);
			this.Name = "ColorScaleSettingsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Color scale settings";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxColorScaleType;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel controlPointsPanel;
		private System.Windows.Forms.ComboBox comboBoxNumberOfSubIntervals;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckBox checkBoxShowIsoAreas;
		private System.Windows.Forms.Button buttonApply;
	}
}