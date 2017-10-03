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
			this.checkBoxInvertVectorArrows = new System.Windows.Forms.CheckBox();
			this.trackBarVectorLengthFactor = new System.Windows.Forms.TrackBar();
			this.labelArrowLengthFactor = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.trackBarVectorLengthFactor)).BeginInit();
			this.SuspendLayout();
			// 
			// checkBoxShowIsoAreas
			// 
			this.checkBoxShowIsoAreas.AutoSize = true;
			this.checkBoxShowIsoAreas.Location = new System.Drawing.Point(8, 28);
			this.checkBoxShowIsoAreas.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxShowIsoAreas.Name = "checkBoxShowIsoAreas";
			this.checkBoxShowIsoAreas.Size = new System.Drawing.Size(127, 21);
			this.checkBoxShowIsoAreas.TabIndex = 0;
			this.checkBoxShowIsoAreas.Text = "Show iso-areas";
			this.checkBoxShowIsoAreas.UseVisualStyleBackColor = true;
			this.checkBoxShowIsoAreas.CheckedChanged += new System.EventHandler(this.checkBoxShowIsoAreas_CheckedChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(4, 53);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(131, 17);
			this.label1.TabIndex = 1;
			this.label1.Text = "Number of intervals";
			// 
			// comboBoxNumberOfSubIntervals
			// 
			this.comboBoxNumberOfSubIntervals.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxNumberOfSubIntervals.FormattingEnabled = true;
			this.comboBoxNumberOfSubIntervals.Location = new System.Drawing.Point(139, 49);
			this.comboBoxNumberOfSubIntervals.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxNumberOfSubIntervals.Name = "comboBoxNumberOfSubIntervals";
			this.comboBoxNumberOfSubIntervals.Size = new System.Drawing.Size(60, 24);
			this.comboBoxNumberOfSubIntervals.TabIndex = 2;
			this.comboBoxNumberOfSubIntervals.SelectedIndexChanged += new System.EventHandler(this.comboBoxNumberOfSubIntervals_SelectedIndexChanged);
			// 
			// labelCaption
			// 
			this.labelCaption.AutoSize = true;
			this.labelCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelCaption.Location = new System.Drawing.Point(0, 0);
			this.labelCaption.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelCaption.Name = "labelCaption";
			this.labelCaption.Size = new System.Drawing.Size(162, 17);
			this.labelCaption.TabIndex = 6;
			this.labelCaption.Text = "Visualization settings";
			// 
			// linkLabelEditColorScale
			// 
			this.linkLabelEditColorScale.AutoSize = true;
			this.linkLabelEditColorScale.Location = new System.Drawing.Point(4, 81);
			this.linkLabelEditColorScale.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.linkLabelEditColorScale.Name = "linkLabelEditColorScale";
			this.linkLabelEditColorScale.Size = new System.Drawing.Size(131, 17);
			this.linkLabelEditColorScale.TabIndex = 7;
			this.linkLabelEditColorScale.TabStop = true;
			this.linkLabelEditColorScale.Text = "Color scale settings";
			this.linkLabelEditColorScale.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelEditColorScale_LinkClicked);
			// 
			// checkBoxInvertVectorArrows
			// 
			this.checkBoxInvertVectorArrows.AutoSize = true;
			this.checkBoxInvertVectorArrows.Location = new System.Drawing.Point(8, 110);
			this.checkBoxInvertVectorArrows.Name = "checkBoxInvertVectorArrows";
			this.checkBoxInvertVectorArrows.Size = new System.Drawing.Size(154, 21);
			this.checkBoxInvertVectorArrows.TabIndex = 8;
			this.checkBoxInvertVectorArrows.Text = "Invert vector arrows";
			this.checkBoxInvertVectorArrows.UseVisualStyleBackColor = true;
			this.checkBoxInvertVectorArrows.CheckedChanged += new System.EventHandler(this.checkBoxInvertVectorArrows_CheckedChanged);
			// 
			// trackBarVectorLengthFactor
			// 
			this.trackBarVectorLengthFactor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBarVectorLengthFactor.LargeChange = 10;
			this.trackBarVectorLengthFactor.Location = new System.Drawing.Point(8, 157);
			this.trackBarVectorLengthFactor.Maximum = 100;
			this.trackBarVectorLengthFactor.Minimum = 1;
			this.trackBarVectorLengthFactor.Name = "trackBarVectorLengthFactor";
			this.trackBarVectorLengthFactor.Size = new System.Drawing.Size(306, 56);
			this.trackBarVectorLengthFactor.SmallChange = 5;
			this.trackBarVectorLengthFactor.TabIndex = 9;
			this.trackBarVectorLengthFactor.Value = 100;
			this.trackBarVectorLengthFactor.ValueChanged += new System.EventHandler(this.trackBarVectorLengthFactor_ValueChanged);
			// 
			// labelArrowLengthFactor
			// 
			this.labelArrowLengthFactor.AutoSize = true;
			this.labelArrowLengthFactor.Location = new System.Drawing.Point(5, 136);
			this.labelArrowLengthFactor.Name = "labelArrowLengthFactor";
			this.labelArrowLengthFactor.Size = new System.Drawing.Size(127, 17);
			this.labelArrowLengthFactor.TabIndex = 10;
			this.labelArrowLengthFactor.Text = "Arrow length factor";
			// 
			// VisualizerSettingsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.labelArrowLengthFactor);
			this.Controls.Add(this.trackBarVectorLengthFactor);
			this.Controls.Add(this.checkBoxInvertVectorArrows);
			this.Controls.Add(this.linkLabelEditColorScale);
			this.Controls.Add(this.labelCaption);
			this.Controls.Add(this.comboBoxNumberOfSubIntervals);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.checkBoxShowIsoAreas);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "VisualizerSettingsControl";
			this.Size = new System.Drawing.Size(329, 345);
			((System.ComponentModel.ISupportInitialize)(this.trackBarVectorLengthFactor)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxShowIsoAreas;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxNumberOfSubIntervals;
		private System.Windows.Forms.Label labelCaption;
		private System.Windows.Forms.LinkLabel linkLabelEditColorScale;
		private System.Windows.Forms.CheckBox checkBoxInvertVectorArrows;
		private System.Windows.Forms.TrackBar trackBarVectorLengthFactor;
		private System.Windows.Forms.Label labelArrowLengthFactor;
	}
}
