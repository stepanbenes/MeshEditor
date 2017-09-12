namespace MeshEditor.DataVisualizer.UI
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
			this.checkBoxCreateDirectoryForSolution = new System.Windows.Forms.CheckBox();
			this.buttonChooseSolutionDirectory = new System.Windows.Forms.Button();
			this.textBoxLocation = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.buttonChooseResultFiles = new System.Windows.Forms.Button();
			this.buttonChooseMeshFile = new System.Windows.Forms.Button();
			this.textBoxResultFiles = new System.Windows.Forms.TextBox();
			this.textBoxMeshFile = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxProjectName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.tabPageCompression = new System.Windows.Forms.TabPage();
			this.compressionParamsControl = new MeshEditor.DataVisualizer.UI.CompressionParamsControl();
			this.tabPageGaussPointsExtrapolation = new System.Windows.Forms.TabPage();
			this.comboBoxGaussPointExtrapolationStrategy = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.buttonImport = new System.Windows.Forms.Button();
			this.buttonClose = new System.Windows.Forms.Button();
			this.tabControl.SuspendLayout();
			this.tabPageResultFiles.SuspendLayout();
			this.tabPageCompression.SuspendLayout();
			this.tabPageGaussPointsExtrapolation.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPageResultFiles);
			this.tabControl.Controls.Add(this.tabPageCompression);
			this.tabControl.Controls.Add(this.tabPageGaussPointsExtrapolation);
			this.tabControl.Location = new System.Drawing.Point(0, 0);
			this.tabControl.Margin = new System.Windows.Forms.Padding(4);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(598, 365);
			this.tabControl.TabIndex = 0;
			// 
			// tabPageResultFiles
			// 
			this.tabPageResultFiles.Controls.Add(this.checkBoxCreateDirectoryForSolution);
			this.tabPageResultFiles.Controls.Add(this.buttonChooseSolutionDirectory);
			this.tabPageResultFiles.Controls.Add(this.textBoxLocation);
			this.tabPageResultFiles.Controls.Add(this.label8);
			this.tabPageResultFiles.Controls.Add(this.buttonChooseResultFiles);
			this.tabPageResultFiles.Controls.Add(this.buttonChooseMeshFile);
			this.tabPageResultFiles.Controls.Add(this.textBoxResultFiles);
			this.tabPageResultFiles.Controls.Add(this.textBoxMeshFile);
			this.tabPageResultFiles.Controls.Add(this.label3);
			this.tabPageResultFiles.Controls.Add(this.label2);
			this.tabPageResultFiles.Controls.Add(this.textBoxProjectName);
			this.tabPageResultFiles.Controls.Add(this.label1);
			this.tabPageResultFiles.Location = new System.Drawing.Point(4, 25);
			this.tabPageResultFiles.Margin = new System.Windows.Forms.Padding(4);
			this.tabPageResultFiles.Name = "tabPageResultFiles";
			this.tabPageResultFiles.Padding = new System.Windows.Forms.Padding(4);
			this.tabPageResultFiles.Size = new System.Drawing.Size(599, 338);
			this.tabPageResultFiles.TabIndex = 0;
			this.tabPageResultFiles.Text = "Result files";
			this.tabPageResultFiles.UseVisualStyleBackColor = true;
			// 
			// checkBoxCreateDirectoryForSolution
			// 
			this.checkBoxCreateDirectoryForSolution.AutoSize = true;
			this.checkBoxCreateDirectoryForSolution.Checked = true;
			this.checkBoxCreateDirectoryForSolution.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxCreateDirectoryForSolution.Location = new System.Drawing.Point(12, 266);
			this.checkBoxCreateDirectoryForSolution.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxCreateDirectoryForSolution.Name = "checkBoxCreateDirectoryForSolution";
			this.checkBoxCreateDirectoryForSolution.Size = new System.Drawing.Size(205, 21);
			this.checkBoxCreateDirectoryForSolution.TabIndex = 11;
			this.checkBoxCreateDirectoryForSolution.Text = "Create directory for solution";
			this.checkBoxCreateDirectoryForSolution.UseVisualStyleBackColor = true;
			// 
			// buttonChooseSolutionDirectory
			// 
			this.buttonChooseSolutionDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonChooseSolutionDirectory.Location = new System.Drawing.Point(531, 231);
			this.buttonChooseSolutionDirectory.Margin = new System.Windows.Forms.Padding(4);
			this.buttonChooseSolutionDirectory.Name = "buttonChooseSolutionDirectory";
			this.buttonChooseSolutionDirectory.Size = new System.Drawing.Size(57, 28);
			this.buttonChooseSolutionDirectory.TabIndex = 10;
			this.buttonChooseSolutionDirectory.Text = "...";
			this.buttonChooseSolutionDirectory.UseVisualStyleBackColor = true;
			this.buttonChooseSolutionDirectory.Click += new System.EventHandler(this.buttonChooseSolutionDirectory_Click);
			// 
			// textBoxLocation
			// 
			this.textBoxLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxLocation.Location = new System.Drawing.Point(12, 234);
			this.textBoxLocation.Margin = new System.Windows.Forms.Padding(4);
			this.textBoxLocation.Name = "textBoxLocation";
			this.textBoxLocation.Size = new System.Drawing.Size(509, 22);
			this.textBoxLocation.TabIndex = 9;
			this.textBoxLocation.TextChanged += new System.EventHandler(this.textBoxLocation_TextChanged);
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(8, 214);
			this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(62, 17);
			this.label8.TabIndex = 8;
			this.label8.Text = "Location";
			// 
			// buttonChooseResultFiles
			// 
			this.buttonChooseResultFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonChooseResultFiles.Location = new System.Drawing.Point(531, 87);
			this.buttonChooseResultFiles.Margin = new System.Windows.Forms.Padding(4);
			this.buttonChooseResultFiles.Name = "buttonChooseResultFiles";
			this.buttonChooseResultFiles.Size = new System.Drawing.Size(57, 28);
			this.buttonChooseResultFiles.TabIndex = 7;
			this.buttonChooseResultFiles.Text = "...";
			this.buttonChooseResultFiles.UseVisualStyleBackColor = true;
			this.buttonChooseResultFiles.Click += new System.EventHandler(this.buttonChooseResultFiles_Click);
			// 
			// buttonChooseMeshFile
			// 
			this.buttonChooseMeshFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonChooseMeshFile.Location = new System.Drawing.Point(531, 39);
			this.buttonChooseMeshFile.Margin = new System.Windows.Forms.Padding(4);
			this.buttonChooseMeshFile.Name = "buttonChooseMeshFile";
			this.buttonChooseMeshFile.Size = new System.Drawing.Size(57, 28);
			this.buttonChooseMeshFile.TabIndex = 6;
			this.buttonChooseMeshFile.Text = "...";
			this.buttonChooseMeshFile.UseVisualStyleBackColor = true;
			this.buttonChooseMeshFile.Click += new System.EventHandler(this.buttonChooseMeshFile_Click);
			// 
			// textBoxResultFiles
			// 
			this.textBoxResultFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxResultFiles.Location = new System.Drawing.Point(12, 90);
			this.textBoxResultFiles.Margin = new System.Windows.Forms.Padding(4);
			this.textBoxResultFiles.Name = "textBoxResultFiles";
			this.textBoxResultFiles.Size = new System.Drawing.Size(509, 22);
			this.textBoxResultFiles.TabIndex = 5;
			// 
			// textBoxMeshFile
			// 
			this.textBoxMeshFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxMeshFile.Location = new System.Drawing.Point(12, 42);
			this.textBoxMeshFile.Margin = new System.Windows.Forms.Padding(4);
			this.textBoxMeshFile.Name = "textBoxMeshFile";
			this.textBoxMeshFile.Size = new System.Drawing.Size(509, 22);
			this.textBoxMeshFile.TabIndex = 4;
			this.textBoxMeshFile.TextChanged += new System.EventHandler(this.textBoxMeshFile_TextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 70);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(77, 17);
			this.label3.TabIndex = 3;
			this.label3.Text = "Result files";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 22);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(64, 17);
			this.label2.TabIndex = 2;
			this.label2.Text = "Mesh file";
			// 
			// textBoxProjectName
			// 
			this.textBoxProjectName.Location = new System.Drawing.Point(12, 186);
			this.textBoxProjectName.Margin = new System.Windows.Forms.Padding(4);
			this.textBoxProjectName.Name = "textBoxProjectName";
			this.textBoxProjectName.Size = new System.Drawing.Size(175, 22);
			this.textBoxProjectName.TabIndex = 1;
			this.textBoxProjectName.TextChanged += new System.EventHandler(this.textBoxProjectName_TextChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 166);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(91, 17);
			this.label1.TabIndex = 0;
			this.label1.Text = "Project name";
			// 
			// tabPageCompression
			// 
			this.tabPageCompression.Controls.Add(this.compressionParamsControl);
			this.tabPageCompression.Location = new System.Drawing.Point(4, 25);
			this.tabPageCompression.Margin = new System.Windows.Forms.Padding(4);
			this.tabPageCompression.Name = "tabPageCompression";
			this.tabPageCompression.Padding = new System.Windows.Forms.Padding(4);
			this.tabPageCompression.Size = new System.Drawing.Size(590, 336);
			this.tabPageCompression.TabIndex = 1;
			this.tabPageCompression.Text = "Compression";
			this.tabPageCompression.UseVisualStyleBackColor = true;
			// 
			// compressionParamsControl
			// 
			this.compressionParamsControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.compressionParamsControl.Location = new System.Drawing.Point(9, 12);
			this.compressionParamsControl.Name = "compressionParamsControl";
			this.compressionParamsControl.Size = new System.Drawing.Size(571, 312);
			this.compressionParamsControl.TabIndex = 10;
			// 
			// tabPageGaussPointsExtrapolation
			// 
			this.tabPageGaussPointsExtrapolation.Controls.Add(this.comboBoxGaussPointExtrapolationStrategy);
			this.tabPageGaussPointsExtrapolation.Controls.Add(this.label7);
			this.tabPageGaussPointsExtrapolation.Location = new System.Drawing.Point(4, 25);
			this.tabPageGaussPointsExtrapolation.Margin = new System.Windows.Forms.Padding(4);
			this.tabPageGaussPointsExtrapolation.Name = "tabPageGaussPointsExtrapolation";
			this.tabPageGaussPointsExtrapolation.Padding = new System.Windows.Forms.Padding(4);
			this.tabPageGaussPointsExtrapolation.Size = new System.Drawing.Size(599, 338);
			this.tabPageGaussPointsExtrapolation.TabIndex = 2;
			this.tabPageGaussPointsExtrapolation.Text = "Gauss points extrapolation";
			this.tabPageGaussPointsExtrapolation.UseVisualStyleBackColor = true;
			// 
			// comboBoxGaussPointExtrapolationStrategy
			// 
			this.comboBoxGaussPointExtrapolationStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxGaussPointExtrapolationStrategy.FormattingEnabled = true;
			this.comboBoxGaussPointExtrapolationStrategy.Items.AddRange(new object[] {
            "Nearest"});
			this.comboBoxGaussPointExtrapolationStrategy.Location = new System.Drawing.Point(15, 36);
			this.comboBoxGaussPointExtrapolationStrategy.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxGaussPointExtrapolationStrategy.Name = "comboBoxGaussPointExtrapolationStrategy";
			this.comboBoxGaussPointExtrapolationStrategy.Size = new System.Drawing.Size(160, 24);
			this.comboBoxGaussPointExtrapolationStrategy.TabIndex = 1;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(11, 16);
			this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(61, 17);
			this.label7.TabIndex = 0;
			this.label7.Text = "Strategy";
			// 
			// buttonImport
			// 
			this.buttonImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonImport.Location = new System.Drawing.Point(374, 372);
			this.buttonImport.Margin = new System.Windows.Forms.Padding(4);
			this.buttonImport.Name = "buttonImport";
			this.buttonImport.Size = new System.Drawing.Size(100, 28);
			this.buttonImport.TabIndex = 1;
			this.buttonImport.Text = "Import";
			this.buttonImport.UseVisualStyleBackColor = true;
			this.buttonImport.Click += new System.EventHandler(this.buttonImport_Click);
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Location = new System.Drawing.Point(482, 372);
			this.buttonClose.Margin = new System.Windows.Forms.Padding(4);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(100, 28);
			this.buttonClose.TabIndex = 2;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			// 
			// ImportFEMResultsForm
			// 
			this.AcceptButton = this.buttonImport;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(598, 415);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.buttonImport);
			this.Controls.Add(this.tabControl);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ImportFEMResultsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Import FEM results";
			this.tabControl.ResumeLayout(false);
			this.tabPageResultFiles.ResumeLayout(false);
			this.tabPageResultFiles.PerformLayout();
			this.tabPageCompression.ResumeLayout(false);
			this.tabPageGaussPointsExtrapolation.ResumeLayout(false);
			this.tabPageGaussPointsExtrapolation.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPageResultFiles;
		private System.Windows.Forms.TabPage tabPageCompression;
		private System.Windows.Forms.Button buttonImport;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.TabPage tabPageGaussPointsExtrapolation;
		private System.Windows.Forms.TextBox textBoxProjectName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonChooseResultFiles;
		private System.Windows.Forms.Button buttonChooseMeshFile;
		private System.Windows.Forms.TextBox textBoxResultFiles;
		private System.Windows.Forms.TextBox textBoxMeshFile;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBoxGaussPointExtrapolationStrategy;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button buttonChooseSolutionDirectory;
		private System.Windows.Forms.TextBox textBoxLocation;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkBoxCreateDirectoryForSolution;
		private CompressionParamsControl compressionParamsControl;
	}
}