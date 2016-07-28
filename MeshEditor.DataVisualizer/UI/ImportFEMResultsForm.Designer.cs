namespace MeshEditor.WinUI
{
	partial class ImportFEMResultsForm
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
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabPageResultFiles = new System.Windows.Forms.TabPage();
			this.buttonChooseResultFiles = new System.Windows.Forms.Button();
			this.buttonChooseMeshFile = new System.Windows.Forms.Button();
			this.textBoxResultFiles = new System.Windows.Forms.TextBox();
			this.textBoxMeshFile = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxProjectName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.tabPageCompression = new System.Windows.Forms.TabPage();
			this.label5 = new System.Windows.Forms.Label();
			this.groupBoxCompressionParameters = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.radioButtonSize = new System.Windows.Forms.RadioButton();
			this.radioButtonQuality = new System.Windows.Forms.RadioButton();
			this.labelCompressionFactor = new System.Windows.Forms.Label();
			this.trackBarCompressionFactor = new System.Windows.Forms.TrackBar();
			this.comboBoxCompressionMethod = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxKeyTimeSteps = new System.Windows.Forms.TextBox();
			this.checkBoxMergeTimeSteps = new System.Windows.Forms.CheckBox();
			this.tabPageGaussPointExtrapolationStrategy = new System.Windows.Forms.TabPage();
			this.buttonImport = new System.Windows.Forms.Button();
			this.buttonClose = new System.Windows.Forms.Button();
			this.tabControl.SuspendLayout();
			this.tabPageResultFiles.SuspendLayout();
			this.tabPageCompression.SuspendLayout();
			this.groupBoxCompressionParameters.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompressionFactor)).BeginInit();
			this.SuspendLayout();
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPageResultFiles);
			this.tabControl.Controls.Add(this.tabPageCompression);
			this.tabControl.Controls.Add(this.tabPageGaussPointExtrapolationStrategy);
			this.tabControl.Location = new System.Drawing.Point(0, 0);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(455, 298);
			this.tabControl.TabIndex = 0;
			// 
			// tabPageResultFiles
			// 
			this.tabPageResultFiles.Controls.Add(this.buttonChooseResultFiles);
			this.tabPageResultFiles.Controls.Add(this.buttonChooseMeshFile);
			this.tabPageResultFiles.Controls.Add(this.textBoxResultFiles);
			this.tabPageResultFiles.Controls.Add(this.textBoxMeshFile);
			this.tabPageResultFiles.Controls.Add(this.label3);
			this.tabPageResultFiles.Controls.Add(this.label2);
			this.tabPageResultFiles.Controls.Add(this.textBoxProjectName);
			this.tabPageResultFiles.Controls.Add(this.label1);
			this.tabPageResultFiles.Location = new System.Drawing.Point(4, 22);
			this.tabPageResultFiles.Name = "tabPageResultFiles";
			this.tabPageResultFiles.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageResultFiles.Size = new System.Drawing.Size(447, 272);
			this.tabPageResultFiles.TabIndex = 0;
			this.tabPageResultFiles.Text = "Result files";
			this.tabPageResultFiles.UseVisualStyleBackColor = true;
			// 
			// buttonChooseResultFiles
			// 
			this.buttonChooseResultFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonChooseResultFiles.Location = new System.Drawing.Point(398, 115);
			this.buttonChooseResultFiles.Name = "buttonChooseResultFiles";
			this.buttonChooseResultFiles.Size = new System.Drawing.Size(43, 23);
			this.buttonChooseResultFiles.TabIndex = 7;
			this.buttonChooseResultFiles.Text = "...";
			this.buttonChooseResultFiles.UseVisualStyleBackColor = true;
			this.buttonChooseResultFiles.Click += new System.EventHandler(this.buttonChooseResultFiles_Click);
			// 
			// buttonChooseMeshFile
			// 
			this.buttonChooseMeshFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonChooseMeshFile.Location = new System.Drawing.Point(398, 76);
			this.buttonChooseMeshFile.Name = "buttonChooseMeshFile";
			this.buttonChooseMeshFile.Size = new System.Drawing.Size(43, 23);
			this.buttonChooseMeshFile.TabIndex = 6;
			this.buttonChooseMeshFile.Text = "...";
			this.buttonChooseMeshFile.UseVisualStyleBackColor = true;
			this.buttonChooseMeshFile.Click += new System.EventHandler(this.buttonChooseMeshFile_Click);
			// 
			// textBoxResultFiles
			// 
			this.textBoxResultFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxResultFiles.Location = new System.Drawing.Point(9, 117);
			this.textBoxResultFiles.Name = "textBoxResultFiles";
			this.textBoxResultFiles.Size = new System.Drawing.Size(383, 20);
			this.textBoxResultFiles.TabIndex = 5;
			// 
			// textBoxMeshFile
			// 
			this.textBoxMeshFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxMeshFile.Location = new System.Drawing.Point(9, 78);
			this.textBoxMeshFile.Name = "textBoxMeshFile";
			this.textBoxMeshFile.Size = new System.Drawing.Size(383, 20);
			this.textBoxMeshFile.TabIndex = 4;
			this.textBoxMeshFile.TextChanged += new System.EventHandler(this.textBoxMeshFile_TextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 101);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(58, 13);
			this.label3.TabIndex = 3;
			this.label3.Text = "Result files";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 62);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(49, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Mesh file";
			// 
			// textBoxProjectName
			// 
			this.textBoxProjectName.Location = new System.Drawing.Point(9, 29);
			this.textBoxProjectName.Name = "textBoxProjectName";
			this.textBoxProjectName.Size = new System.Drawing.Size(132, 20);
			this.textBoxProjectName.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(69, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Project name";
			// 
			// tabPageCompression
			// 
			this.tabPageCompression.Controls.Add(this.label5);
			this.tabPageCompression.Controls.Add(this.groupBoxCompressionParameters);
			this.tabPageCompression.Controls.Add(this.comboBoxCompressionMethod);
			this.tabPageCompression.Controls.Add(this.label4);
			this.tabPageCompression.Controls.Add(this.textBoxKeyTimeSteps);
			this.tabPageCompression.Controls.Add(this.checkBoxMergeTimeSteps);
			this.tabPageCompression.Location = new System.Drawing.Point(4, 22);
			this.tabPageCompression.Name = "tabPageCompression";
			this.tabPageCompression.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageCompression.Size = new System.Drawing.Size(447, 272);
			this.tabPageCompression.TabIndex = 1;
			this.tabPageCompression.Text = "Compression";
			this.tabPageCompression.UseVisualStyleBackColor = true;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(8, 217);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(75, 13);
			this.label5.TabIndex = 9;
			this.label5.Text = "Key time steps";
			// 
			// groupBoxCompressionParameters
			// 
			this.groupBoxCompressionParameters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxCompressionParameters.Controls.Add(this.label6);
			this.groupBoxCompressionParameters.Controls.Add(this.radioButtonSize);
			this.groupBoxCompressionParameters.Controls.Add(this.radioButtonQuality);
			this.groupBoxCompressionParameters.Controls.Add(this.labelCompressionFactor);
			this.groupBoxCompressionParameters.Controls.Add(this.trackBarCompressionFactor);
			this.groupBoxCompressionParameters.Location = new System.Drawing.Point(11, 57);
			this.groupBoxCompressionParameters.Name = "groupBoxCompressionParameters";
			this.groupBoxCompressionParameters.Size = new System.Drawing.Size(426, 121);
			this.groupBoxCompressionParameters.TabIndex = 5;
			this.groupBoxCompressionParameters.TabStop = false;
			this.groupBoxCompressionParameters.Text = "Parameters";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(7, 84);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(62, 13);
			this.label6.TabIndex = 3;
			this.label6.Text = "Preference:";
			// 
			// radioButtonSize
			// 
			this.radioButtonSize.AutoSize = true;
			this.radioButtonSize.Location = new System.Drawing.Point(143, 82);
			this.radioButtonSize.Name = "radioButtonSize";
			this.radioButtonSize.Size = new System.Drawing.Size(45, 17);
			this.radioButtonSize.TabIndex = 8;
			this.radioButtonSize.TabStop = true;
			this.radioButtonSize.Text = "Size";
			this.radioButtonSize.UseVisualStyleBackColor = true;
			// 
			// radioButtonQuality
			// 
			this.radioButtonQuality.AutoSize = true;
			this.radioButtonQuality.Location = new System.Drawing.Point(80, 82);
			this.radioButtonQuality.Name = "radioButtonQuality";
			this.radioButtonQuality.Size = new System.Drawing.Size(57, 17);
			this.radioButtonQuality.TabIndex = 7;
			this.radioButtonQuality.TabStop = true;
			this.radioButtonQuality.Text = "Quality";
			this.radioButtonQuality.UseVisualStyleBackColor = true;
			// 
			// labelCompressionFactor
			// 
			this.labelCompressionFactor.AutoSize = true;
			this.labelCompressionFactor.Location = new System.Drawing.Point(7, 20);
			this.labelCompressionFactor.Name = "labelCompressionFactor";
			this.labelCompressionFactor.Size = new System.Drawing.Size(97, 13);
			this.labelCompressionFactor.TabIndex = 6;
			this.labelCompressionFactor.Text = "Compression factor";
			// 
			// trackBarCompressionFactor
			// 
			this.trackBarCompressionFactor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBarCompressionFactor.LargeChange = 10;
			this.trackBarCompressionFactor.Location = new System.Drawing.Point(6, 36);
			this.trackBarCompressionFactor.Maximum = 100;
			this.trackBarCompressionFactor.Name = "trackBarCompressionFactor";
			this.trackBarCompressionFactor.Size = new System.Drawing.Size(413, 45);
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
			this.comboBoxCompressionMethod.Location = new System.Drawing.Point(11, 30);
			this.comboBoxCompressionMethod.Name = "comboBoxCompressionMethod";
			this.comboBoxCompressionMethod.Size = new System.Drawing.Size(121, 21);
			this.comboBoxCompressionMethod.TabIndex = 1;
			this.comboBoxCompressionMethod.SelectedIndexChanged += new System.EventHandler(this.comboBoxCompressionMethod_SelectedIndexChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(8, 13);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(105, 13);
			this.label4.TabIndex = 0;
			this.label4.Text = "Compression method";
			// 
			// textBoxKeyTimeSteps
			// 
			this.textBoxKeyTimeSteps.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxKeyTimeSteps.Location = new System.Drawing.Point(11, 233);
			this.textBoxKeyTimeSteps.Name = "textBoxKeyTimeSteps";
			this.textBoxKeyTimeSteps.Size = new System.Drawing.Size(426, 20);
			this.textBoxKeyTimeSteps.TabIndex = 3;
			// 
			// checkBoxMergeTimeSteps
			// 
			this.checkBoxMergeTimeSteps.AutoSize = true;
			this.checkBoxMergeTimeSteps.Location = new System.Drawing.Point(11, 197);
			this.checkBoxMergeTimeSteps.Name = "checkBoxMergeTimeSteps";
			this.checkBoxMergeTimeSteps.Size = new System.Drawing.Size(106, 17);
			this.checkBoxMergeTimeSteps.TabIndex = 4;
			this.checkBoxMergeTimeSteps.Text = "Merge time steps";
			this.checkBoxMergeTimeSteps.UseVisualStyleBackColor = true;
			this.checkBoxMergeTimeSteps.CheckedChanged += new System.EventHandler(this.checkBoxMergeTimeSteps_CheckedChanged);
			// 
			// tabPageGaussPointExtrapolationStrategy
			// 
			this.tabPageGaussPointExtrapolationStrategy.Location = new System.Drawing.Point(4, 22);
			this.tabPageGaussPointExtrapolationStrategy.Name = "tabPageGaussPointExtrapolationStrategy";
			this.tabPageGaussPointExtrapolationStrategy.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageGaussPointExtrapolationStrategy.Size = new System.Drawing.Size(447, 272);
			this.tabPageGaussPointExtrapolationStrategy.TabIndex = 2;
			this.tabPageGaussPointExtrapolationStrategy.Text = "Gauss point extrapolation strategy";
			this.tabPageGaussPointExtrapolationStrategy.UseVisualStyleBackColor = true;
			// 
			// buttonImport
			// 
			this.buttonImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonImport.Location = new System.Drawing.Point(287, 304);
			this.buttonImport.Name = "buttonImport";
			this.buttonImport.Size = new System.Drawing.Size(75, 23);
			this.buttonImport.TabIndex = 1;
			this.buttonImport.Text = "Import";
			this.buttonImport.UseVisualStyleBackColor = true;
			this.buttonImport.Click += new System.EventHandler(this.buttonImport_Click);
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Location = new System.Drawing.Point(368, 304);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(75, 23);
			this.buttonClose.TabIndex = 2;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			// 
			// ImportFEMResultsForm
			// 
			this.AcceptButton = this.buttonImport;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(455, 339);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.buttonImport);
			this.Controls.Add(this.tabControl);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ImportFEMResultsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Import FEM results";
			this.tabControl.ResumeLayout(false);
			this.tabPageResultFiles.ResumeLayout(false);
			this.tabPageResultFiles.PerformLayout();
			this.tabPageCompression.ResumeLayout(false);
			this.tabPageCompression.PerformLayout();
			this.groupBoxCompressionParameters.ResumeLayout(false);
			this.groupBoxCompressionParameters.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompressionFactor)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPageResultFiles;
		private System.Windows.Forms.TabPage tabPageCompression;
		private System.Windows.Forms.Button buttonImport;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.TabPage tabPageGaussPointExtrapolationStrategy;
		private System.Windows.Forms.TextBox textBoxProjectName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonChooseResultFiles;
		private System.Windows.Forms.Button buttonChooseMeshFile;
		private System.Windows.Forms.TextBox textBoxResultFiles;
		private System.Windows.Forms.TextBox textBoxMeshFile;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBoxCompressionMethod;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textBoxKeyTimeSteps;
		private System.Windows.Forms.CheckBox checkBoxMergeTimeSteps;
		private System.Windows.Forms.GroupBox groupBoxCompressionParameters;
		private System.Windows.Forms.TrackBar trackBarCompressionFactor;
		private System.Windows.Forms.RadioButton radioButtonSize;
		private System.Windows.Forms.RadioButton radioButtonQuality;
		private System.Windows.Forms.Label labelCompressionFactor;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
	}
}