namespace MeshEditor.DataVisualizer.UI
{
	partial class SolutionBrowserForm
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
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabPageLocalSolutions = new System.Windows.Forms.TabPage();
			this.buttonChangeDefaultSolutionDirectory = new System.Windows.Forms.Button();
			this.buttonDeleteLocalSolution = new System.Windows.Forms.Button();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.localSolutionListView = new MeshEditor.DataVisualizer.UI.SolutionListView();
			this.tabControl.SuspendLayout();
			this.tabPageLocalSolutions.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonOk
			// 
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(509, 519);
			this.buttonOk.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(100, 28);
			this.buttonOk.TabIndex = 1;
			this.buttonOk.Text = "OK";
			this.buttonOk.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(617, 519);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(100, 28);
			this.buttonCancel.TabIndex = 2;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonRemoteSolutionOpenInBrowser
			// 
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPageLocalSolutions);
			this.tabControl.Location = new System.Drawing.Point(0, 0);
			this.tabControl.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(731, 511);
			this.tabControl.TabIndex = 4;
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
			// 
			// tabPageLocalSolutions
			// 
			this.tabPageLocalSolutions.Controls.Add(this.buttonChangeDefaultSolutionDirectory);
			this.tabPageLocalSolutions.Controls.Add(this.localSolutionListView);
			this.tabPageLocalSolutions.Controls.Add(this.buttonDeleteLocalSolution);
			this.tabPageLocalSolutions.Location = new System.Drawing.Point(4, 25);
			this.tabPageLocalSolutions.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.tabPageLocalSolutions.Name = "tabPageLocalSolutions";
			this.tabPageLocalSolutions.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.tabPageLocalSolutions.Size = new System.Drawing.Size(723, 482);
			this.tabPageLocalSolutions.TabIndex = 0;
			this.tabPageLocalSolutions.Text = "Local solutions";
			this.tabPageLocalSolutions.UseVisualStyleBackColor = true;
			// 
			// buttonChangeDefaultSolutionDirectory
			// 
			this.buttonChangeDefaultSolutionDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonChangeDefaultSolutionDirectory.Location = new System.Drawing.Point(8, 443);
			this.buttonChangeDefaultSolutionDirectory.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.buttonChangeDefaultSolutionDirectory.Name = "buttonChangeDefaultSolutionDirectory";
			this.buttonChangeDefaultSolutionDirectory.Size = new System.Drawing.Size(256, 28);
			this.buttonChangeDefaultSolutionDirectory.TabIndex = 5;
			this.buttonChangeDefaultSolutionDirectory.Text = "Change default solution directory...";
			this.buttonChangeDefaultSolutionDirectory.UseVisualStyleBackColor = true;
			this.buttonChangeDefaultSolutionDirectory.Click += new System.EventHandler(this.buttonChangeDefaultSolutionDirectory_Click);
			// 
			// buttonDeleteLocalSolution
			// 
			this.buttonDeleteLocalSolution.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonDeleteLocalSolution.Location = new System.Drawing.Point(612, 443);
			this.buttonDeleteLocalSolution.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.buttonDeleteLocalSolution.Name = "buttonDeleteLocalSolution";
			this.buttonDeleteLocalSolution.Size = new System.Drawing.Size(100, 28);
			this.buttonDeleteLocalSolution.TabIndex = 2;
			this.buttonDeleteLocalSolution.Text = "Delete";
			this.buttonDeleteLocalSolution.UseVisualStyleBackColor = true;
			this.buttonDeleteLocalSolution.Click += new System.EventHandler(this.buttonDeleteLocalSolution_Click);
			// 
			// tabPageRemoteSolutions
			// 
			// 
			// buttonDeleteRemoteSolution
			// 
			// 
			// buttonBrowse
			// 
			this.buttonBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonBrowse.Location = new System.Drawing.Point(12, 519);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(99, 28);
			this.buttonBrowse.TabIndex = 5;
			this.buttonBrowse.Text = "Browse...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			// 
			// localSolutionListView
			// 
			this.localSolutionListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.localSolutionListView.Location = new System.Drawing.Point(8, 7);
			this.localSolutionListView.Margin = new System.Windows.Forms.Padding(5);
			this.localSolutionListView.Name = "localSolutionListView";
			this.localSolutionListView.Notification = "Notification label";
			this.localSolutionListView.Size = new System.Drawing.Size(704, 428);
			this.localSolutionListView.TabIndex = 4;
			this.localSolutionListView.SelectedSolutionChanged += new System.EventHandler(this.localSolutionListView_SelectedSolutionChanged);
			this.localSolutionListView.SolutionListDoubleClick += new System.EventHandler(this.localSolutionListView_SolutionListDoubleClick);

			// 
			// SolutionBrowserForm
			// 
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(731, 562);
			this.Controls.Add(this.buttonBrowse);
			this.Controls.Add(this.tabControl);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.Name = "SolutionBrowserForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Open solution";
			this.tabControl.ResumeLayout(false);
			this.tabPageLocalSolutions.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPageLocalSolutions;
		private System.Windows.Forms.Button buttonDeleteLocalSolution;
		private SolutionListView localSolutionListView;
		private System.Windows.Forms.Button buttonChangeDefaultSolutionDirectory;
		private System.Windows.Forms.Button buttonBrowse;
	}
}