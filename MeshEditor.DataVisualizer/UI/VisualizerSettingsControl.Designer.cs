namespace MeshEditor.DataVisualizer.UI
{
	partial class VisualizerSettingsControl
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
			this.checkBoxShowIsoAreas = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxNumberOfSubIntervals = new System.Windows.Forms.ComboBox();
			this.labelCaption = new System.Windows.Forms.Label();
			this.linkLabelEditColorScale = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// checkBoxShowIsoAreas
			// 
			this.checkBoxShowIsoAreas.AutoSize = true;
			this.checkBoxShowIsoAreas.Location = new System.Drawing.Point(6, 23);
			this.checkBoxShowIsoAreas.Name = "checkBoxShowIsoAreas";
			this.checkBoxShowIsoAreas.Size = new System.Drawing.Size(98, 17);
			this.checkBoxShowIsoAreas.TabIndex = 0;
			this.checkBoxShowIsoAreas.Text = "Show iso-areas";
			this.checkBoxShowIsoAreas.UseVisualStyleBackColor = true;
			this.checkBoxShowIsoAreas.CheckedChanged += new System.EventHandler(this.checkBoxShowIsoAreas_CheckedChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 43);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(98, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Number of intervals";
			// 
			// comboBoxNumberOfSubIntervals
			// 
			this.comboBoxNumberOfSubIntervals.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxNumberOfSubIntervals.FormattingEnabled = true;
			this.comboBoxNumberOfSubIntervals.Location = new System.Drawing.Point(104, 40);
			this.comboBoxNumberOfSubIntervals.Name = "comboBoxNumberOfSubIntervals";
			this.comboBoxNumberOfSubIntervals.Size = new System.Drawing.Size(46, 21);
			this.comboBoxNumberOfSubIntervals.TabIndex = 2;
			this.comboBoxNumberOfSubIntervals.SelectedIndexChanged += new System.EventHandler(this.comboBoxNumberOfSubIntervals_SelectedIndexChanged);
			// 
			// labelCaption
			// 
			this.labelCaption.AutoSize = true;
			this.labelCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelCaption.Location = new System.Drawing.Point(0, 0);
			this.labelCaption.Name = "labelCaption";
			this.labelCaption.Size = new System.Drawing.Size(126, 13);
			this.labelCaption.TabIndex = 6;
			this.labelCaption.Text = "Visualization settings";
			// 
			// linkLabelEditColorScale
			// 
			this.linkLabelEditColorScale.AutoSize = true;
			this.linkLabelEditColorScale.Location = new System.Drawing.Point(3, 68);
			this.linkLabelEditColorScale.Name = "linkLabelEditColorScale";
			this.linkLabelEditColorScale.Size = new System.Drawing.Size(79, 13);
			this.linkLabelEditColorScale.TabIndex = 7;
			this.linkLabelEditColorScale.TabStop = true;
			this.linkLabelEditColorScale.Text = "Edit color scale";
			this.linkLabelEditColorScale.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelEditColorScale_LinkClicked);
			// 
			// VisualizerSettingsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.linkLabelEditColorScale);
			this.Controls.Add(this.labelCaption);
			this.Controls.Add(this.comboBoxNumberOfSubIntervals);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.checkBoxShowIsoAreas);
			this.Name = "VisualizerSettingsControl";
			this.Size = new System.Drawing.Size(247, 280);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxShowIsoAreas;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxNumberOfSubIntervals;
		private System.Windows.Forms.Label labelCaption;
		private System.Windows.Forms.LinkLabel linkLabelEditColorScale;
	}
}
