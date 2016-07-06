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
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPageResultFiles = new System.Windows.Forms.TabPage();
			this.tabPageCompression = new System.Windows.Forms.TabPage();
			this.tabPageGaussPointExtrapolationStrategy = new System.Windows.Forms.TabPage();
			this.buttonImport = new System.Windows.Forms.Button();
			this.buttonClose = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxProjectName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.textBoxMeshFile = new System.Windows.Forms.TextBox();
			this.textBoxResultFiles = new System.Windows.Forms.TextBox();
			this.buttonChooseMeshFile = new System.Windows.Forms.Button();
			this.buttonChooseResultFiles = new System.Windows.Forms.Button();
			this.tabControl1.SuspendLayout();
			this.tabPageResultFiles.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.tabPageResultFiles);
			this.tabControl1.Controls.Add(this.tabPageCompression);
			this.tabControl1.Controls.Add(this.tabPageGaussPointExtrapolationStrategy);
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(455, 194);
			this.tabControl1.TabIndex = 0;
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
			this.tabPageResultFiles.Size = new System.Drawing.Size(447, 168);
			this.tabPageResultFiles.TabIndex = 0;
			this.tabPageResultFiles.Text = "Result files";
			this.tabPageResultFiles.UseVisualStyleBackColor = true;
			// 
			// tabPageCompression
			// 
			this.tabPageCompression.Location = new System.Drawing.Point(4, 22);
			this.tabPageCompression.Name = "tabPageCompression";
			this.tabPageCompression.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageCompression.Size = new System.Drawing.Size(386, 322);
			this.tabPageCompression.TabIndex = 1;
			this.tabPageCompression.Text = "Compression";
			this.tabPageCompression.UseVisualStyleBackColor = true;
			// 
			// tabPageGaussPointExtrapolationStrategy
			// 
			this.tabPageGaussPointExtrapolationStrategy.Location = new System.Drawing.Point(4, 22);
			this.tabPageGaussPointExtrapolationStrategy.Name = "tabPageGaussPointExtrapolationStrategy";
			this.tabPageGaussPointExtrapolationStrategy.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageGaussPointExtrapolationStrategy.Size = new System.Drawing.Size(386, 322);
			this.tabPageGaussPointExtrapolationStrategy.TabIndex = 2;
			this.tabPageGaussPointExtrapolationStrategy.Text = "Gauss point extrapolation strategy";
			this.tabPageGaussPointExtrapolationStrategy.UseVisualStyleBackColor = true;
			// 
			// buttonImport
			// 
			this.buttonImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonImport.Location = new System.Drawing.Point(287, 200);
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
			this.buttonClose.Location = new System.Drawing.Point(368, 200);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(75, 23);
			this.buttonClose.TabIndex = 2;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
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
			// textBoxProjectName
			// 
			this.textBoxProjectName.Location = new System.Drawing.Point(9, 29);
			this.textBoxProjectName.Name = "textBoxProjectName";
			this.textBoxProjectName.Size = new System.Drawing.Size(132, 20);
			this.textBoxProjectName.TabIndex = 1;
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
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 101);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(58, 13);
			this.label3.TabIndex = 3;
			this.label3.Text = "Result files";
			// 
			// textBoxMeshFile
			// 
			this.textBoxMeshFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxMeshFile.Location = new System.Drawing.Point(9, 78);
			this.textBoxMeshFile.Name = "textBoxMeshFile";
			this.textBoxMeshFile.Size = new System.Drawing.Size(383, 20);
			this.textBoxMeshFile.TabIndex = 4;
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
			// ImportFEMResultsForm
			// 
			this.AcceptButton = this.buttonImport;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(455, 235);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.buttonImport);
			this.Controls.Add(this.tabControl1);
			this.Name = "ImportFEMResultsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Import FEM results";
			this.tabControl1.ResumeLayout(false);
			this.tabPageResultFiles.ResumeLayout(false);
			this.tabPageResultFiles.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
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
	}
}