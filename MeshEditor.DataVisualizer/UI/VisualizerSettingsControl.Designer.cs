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
			this.buttonEditColorScale = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// checkBoxShowIsoAreas
			// 
			this.checkBoxShowIsoAreas.AutoSize = true;
			this.checkBoxShowIsoAreas.Location = new System.Drawing.Point(3, 3);
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
			this.label1.Location = new System.Drawing.Point(0, 23);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(98, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Number of intervals";
			// 
			// comboBoxNumberOfSubIntervals
			// 
			this.comboBoxNumberOfSubIntervals.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxNumberOfSubIntervals.FormattingEnabled = true;
			this.comboBoxNumberOfSubIntervals.Location = new System.Drawing.Point(3, 39);
			this.comboBoxNumberOfSubIntervals.Name = "comboBoxNumberOfSubIntervals";
			this.comboBoxNumberOfSubIntervals.Size = new System.Drawing.Size(46, 21);
			this.comboBoxNumberOfSubIntervals.TabIndex = 2;
			this.comboBoxNumberOfSubIntervals.SelectedIndexChanged += new System.EventHandler(this.comboBoxNumberOfSubIntervals_SelectedIndexChanged);
			// 
			// buttonEditColorScale
			// 
			this.buttonEditColorScale.Location = new System.Drawing.Point(3, 77);
			this.buttonEditColorScale.Name = "buttonEditColorScale";
			this.buttonEditColorScale.Size = new System.Drawing.Size(107, 23);
			this.buttonEditColorScale.TabIndex = 5;
			this.buttonEditColorScale.Text = "Edit color scale";
			this.buttonEditColorScale.UseVisualStyleBackColor = true;
			this.buttonEditColorScale.Click += new System.EventHandler(this.buttonEditColorScale_Click);
			// 
			// VisualizerSettingsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.buttonEditColorScale);
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
		private System.Windows.Forms.Button buttonEditColorScale;
	}
}
