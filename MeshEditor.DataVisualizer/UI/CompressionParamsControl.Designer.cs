namespace MeshEditor.DataVisualizer.UI
{
	partial class CompressionParamsControl
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
			this.label5 = new System.Windows.Forms.Label();
			this.groupBoxSVDCompressionParameters = new System.Windows.Forms.GroupBox();
			this.checkBoxSVDParameterRandomized = new System.Windows.Forms.CheckBox();
			this.label6 = new System.Windows.Forms.Label();
			this.radioButtonSize = new System.Windows.Forms.RadioButton();
			this.radioButtonQuality = new System.Windows.Forms.RadioButton();
			this.labelCompressionFactor = new System.Windows.Forms.Label();
			this.trackBarCompressionFactor = new System.Windows.Forms.TrackBar();
			this.comboBoxCompressionMethod = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxKeyTimeSteps = new System.Windows.Forms.TextBox();
			this.checkBoxMergeTimeSteps = new System.Windows.Forms.CheckBox();
			this.groupBoxSVDCompressionParameters.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompressionFactor)).BeginInit();
			this.SuspendLayout();
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(3, 262);
			this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(100, 17);
			this.label5.TabIndex = 15;
			this.label5.Text = "Key time steps";
			// 
			// groupBoxSVDCompressionParameters
			// 
			this.groupBoxSVDCompressionParameters.Controls.Add(this.checkBoxSVDParameterRandomized);
			this.groupBoxSVDCompressionParameters.Controls.Add(this.label6);
			this.groupBoxSVDCompressionParameters.Controls.Add(this.radioButtonSize);
			this.groupBoxSVDCompressionParameters.Controls.Add(this.radioButtonQuality);
			this.groupBoxSVDCompressionParameters.Controls.Add(this.labelCompressionFactor);
			this.groupBoxSVDCompressionParameters.Controls.Add(this.trackBarCompressionFactor);
			this.groupBoxSVDCompressionParameters.Location = new System.Drawing.Point(6, 56);
			this.groupBoxSVDCompressionParameters.Margin = new System.Windows.Forms.Padding(4);
			this.groupBoxSVDCompressionParameters.Name = "groupBoxSVDCompressionParameters";
			this.groupBoxSVDCompressionParameters.Padding = new System.Windows.Forms.Padding(4);
			this.groupBoxSVDCompressionParameters.Size = new System.Drawing.Size(559, 167);
			this.groupBoxSVDCompressionParameters.TabIndex = 14;
			this.groupBoxSVDCompressionParameters.TabStop = false;
			this.groupBoxSVDCompressionParameters.Text = "SVD parameters";
			// 
			// checkBoxSVDParameterRandomized
			// 
			this.checkBoxSVDParameterRandomized.AutoSize = true;
			this.checkBoxSVDParameterRandomized.Location = new System.Drawing.Point(7, 139);
			this.checkBoxSVDParameterRandomized.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxSVDParameterRandomized.Name = "checkBoxSVDParameterRandomized";
			this.checkBoxSVDParameterRandomized.Size = new System.Drawing.Size(402, 21);
			this.checkBoxSVDParameterRandomized.TabIndex = 9;
			this.checkBoxSVDParameterRandomized.Text = "Use Randomized SVD to accelerate compression algorithm";
			this.checkBoxSVDParameterRandomized.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(8, 105);
			this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(82, 17);
			this.label6.TabIndex = 3;
			this.label6.Text = "Preference:";
			// 
			// radioButtonSize
			// 
			this.radioButtonSize.AutoSize = true;
			this.radioButtonSize.Location = new System.Drawing.Point(190, 103);
			this.radioButtonSize.Margin = new System.Windows.Forms.Padding(4);
			this.radioButtonSize.Name = "radioButtonSize";
			this.radioButtonSize.Size = new System.Drawing.Size(56, 21);
			this.radioButtonSize.TabIndex = 8;
			this.radioButtonSize.TabStop = true;
			this.radioButtonSize.Text = "Size";
			this.radioButtonSize.UseVisualStyleBackColor = true;
			// 
			// radioButtonQuality
			// 
			this.radioButtonQuality.AutoSize = true;
			this.radioButtonQuality.Location = new System.Drawing.Point(106, 103);
			this.radioButtonQuality.Margin = new System.Windows.Forms.Padding(4);
			this.radioButtonQuality.Name = "radioButtonQuality";
			this.radioButtonQuality.Size = new System.Drawing.Size(73, 21);
			this.radioButtonQuality.TabIndex = 7;
			this.radioButtonQuality.TabStop = true;
			this.radioButtonQuality.Text = "Quality";
			this.radioButtonQuality.UseVisualStyleBackColor = true;
			// 
			// labelCompressionFactor
			// 
			this.labelCompressionFactor.AutoSize = true;
			this.labelCompressionFactor.Location = new System.Drawing.Point(8, 27);
			this.labelCompressionFactor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelCompressionFactor.Name = "labelCompressionFactor";
			this.labelCompressionFactor.Size = new System.Drawing.Size(130, 17);
			this.labelCompressionFactor.TabIndex = 6;
			this.labelCompressionFactor.Text = "Compression factor";
			// 
			// trackBarCompressionFactor
			// 
			this.trackBarCompressionFactor.LargeChange = 10;
			this.trackBarCompressionFactor.Location = new System.Drawing.Point(7, 46);
			this.trackBarCompressionFactor.Margin = new System.Windows.Forms.Padding(4);
			this.trackBarCompressionFactor.Maximum = 100;
			this.trackBarCompressionFactor.Name = "trackBarCompressionFactor";
			this.trackBarCompressionFactor.Size = new System.Drawing.Size(543, 56);
			this.trackBarCompressionFactor.TabIndex = 5;
			this.trackBarCompressionFactor.ValueChanged += new System.EventHandler(this.trackBarCompressionFactor_ValueChanged);
			// 
			// comboBoxCompressionMethod
			// 
			this.comboBoxCompressionMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxCompressionMethod.FormattingEnabled = true;
			this.comboBoxCompressionMethod.Items.AddRange(new object[] {
            "None",
            "SVD"});
			this.comboBoxCompressionMethod.Location = new System.Drawing.Point(6, 24);
			this.comboBoxCompressionMethod.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxCompressionMethod.Name = "comboBoxCompressionMethod";
			this.comboBoxCompressionMethod.Size = new System.Drawing.Size(160, 24);
			this.comboBoxCompressionMethod.TabIndex = 11;
			this.comboBoxCompressionMethod.SelectedIndexChanged += new System.EventHandler(this.comboBoxCompressionMethod_SelectedIndexChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(3, 3);
			this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(141, 17);
			this.label4.TabIndex = 10;
			this.label4.Text = "Compression method";
			// 
			// textBoxKeyTimeSteps
			// 
			this.textBoxKeyTimeSteps.Location = new System.Drawing.Point(6, 284);
			this.textBoxKeyTimeSteps.Margin = new System.Windows.Forms.Padding(4);
			this.textBoxKeyTimeSteps.Name = "textBoxKeyTimeSteps";
			this.textBoxKeyTimeSteps.Size = new System.Drawing.Size(559, 22);
			this.textBoxKeyTimeSteps.TabIndex = 12;
			// 
			// checkBoxMergeTimeSteps
			// 
			this.checkBoxMergeTimeSteps.AutoSize = true;
			this.checkBoxMergeTimeSteps.Location = new System.Drawing.Point(6, 237);
			this.checkBoxMergeTimeSteps.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxMergeTimeSteps.Name = "checkBoxMergeTimeSteps";
			this.checkBoxMergeTimeSteps.Size = new System.Drawing.Size(138, 21);
			this.checkBoxMergeTimeSteps.TabIndex = 13;
			this.checkBoxMergeTimeSteps.Text = "Merge time steps";
			this.checkBoxMergeTimeSteps.UseVisualStyleBackColor = true;
			this.checkBoxMergeTimeSteps.CheckedChanged += new System.EventHandler(this.checkBoxMergeTimeSteps_CheckedChanged);
			// 
			// CompressionParamsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label5);
			this.Controls.Add(this.groupBoxSVDCompressionParameters);
			this.Controls.Add(this.comboBoxCompressionMethod);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.textBoxKeyTimeSteps);
			this.Controls.Add(this.checkBoxMergeTimeSteps);
			this.Name = "CompressionParamsControl";
			this.Size = new System.Drawing.Size(572, 314);
			this.groupBoxSVDCompressionParameters.ResumeLayout(false);
			this.groupBoxSVDCompressionParameters.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompressionFactor)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.GroupBox groupBoxSVDCompressionParameters;
		private System.Windows.Forms.CheckBox checkBoxSVDParameterRandomized;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.RadioButton radioButtonSize;
		private System.Windows.Forms.RadioButton radioButtonQuality;
		private System.Windows.Forms.Label labelCompressionFactor;
		private System.Windows.Forms.TrackBar trackBarCompressionFactor;
		private System.Windows.Forms.ComboBox comboBoxCompressionMethod;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textBoxKeyTimeSteps;
		private System.Windows.Forms.CheckBox checkBoxMergeTimeSteps;
	}
}
