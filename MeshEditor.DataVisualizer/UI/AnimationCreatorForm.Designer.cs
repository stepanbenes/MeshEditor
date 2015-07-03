namespace MeshEditor.DataVisualizer.UI
{
	partial class AnimationCreatorForm
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
			this.buttonCreateAnimation = new System.Windows.Forms.Button();
			this.textBoxFPS = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.checkBoxRepeat = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBoxStep = new System.Windows.Forms.ComboBox();
			this.checkBoxSaveToFiles = new System.Windows.Forms.CheckBox();
			this.listViewTimeSteps = new System.Windows.Forms.ListView();
			this.columnHeaderDummy = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// buttonCreateAnimation
			// 
			this.buttonCreateAnimation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonCreateAnimation.Location = new System.Drawing.Point(12, 400);
			this.buttonCreateAnimation.Name = "buttonCreateAnimation";
			this.buttonCreateAnimation.Size = new System.Drawing.Size(75, 23);
			this.buttonCreateAnimation.TabIndex = 0;
			this.buttonCreateAnimation.Text = "Start";
			this.buttonCreateAnimation.UseVisualStyleBackColor = true;
			this.buttonCreateAnimation.Click += new System.EventHandler(this.buttonCreateAnimation_Click);
			// 
			// textBoxFPS
			// 
			this.textBoxFPS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBoxFPS.Location = new System.Drawing.Point(12, 354);
			this.textBoxFPS.Name = "textBoxFPS";
			this.textBoxFPS.Size = new System.Drawing.Size(100, 20);
			this.textBoxFPS.TabIndex = 9;
			this.textBoxFPS.Text = "5";
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 339);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(97, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "Frames per second";
			// 
			// progressBar
			// 
			this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar.Location = new System.Drawing.Point(12, 380);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(262, 14);
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar.TabIndex = 11;
			this.progressBar.Visible = false;
			// 
			// checkBoxRepeat
			// 
			this.checkBoxRepeat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxRepeat.AutoSize = true;
			this.checkBoxRepeat.Location = new System.Drawing.Point(93, 404);
			this.checkBoxRepeat.Name = "checkBoxRepeat";
			this.checkBoxRepeat.Size = new System.Drawing.Size(61, 17);
			this.checkBoxRepeat.TabIndex = 12;
			this.checkBoxRepeat.Text = "Repeat";
			this.checkBoxRepeat.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(35, 13);
			this.label1.TabIndex = 14;
			this.label1.Text = "Times";
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(195, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(74, 13);
			this.label2.TabIndex = 15;
			this.label2.Text = "Selection step";
			// 
			// comboBoxStep
			// 
			this.comboBoxStep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxStep.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxStep.FormattingEnabled = true;
			this.comboBoxStep.Location = new System.Drawing.Point(198, 25);
			this.comboBoxStep.Name = "comboBoxStep";
			this.comboBoxStep.Size = new System.Drawing.Size(76, 21);
			this.comboBoxStep.TabIndex = 16;
			this.comboBoxStep.SelectedIndexChanged += new System.EventHandler(this.comboBoxStep_SelectedIndexChanged);
			// 
			// checkBoxSaveToFiles
			// 
			this.checkBoxSaveToFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxSaveToFiles.AutoSize = true;
			this.checkBoxSaveToFiles.Checked = true;
			this.checkBoxSaveToFiles.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxSaveToFiles.Location = new System.Drawing.Point(160, 404);
			this.checkBoxSaveToFiles.Name = "checkBoxSaveToFiles";
			this.checkBoxSaveToFiles.Size = new System.Drawing.Size(84, 17);
			this.checkBoxSaveToFiles.TabIndex = 17;
			this.checkBoxSaveToFiles.Text = "Save to files";
			this.checkBoxSaveToFiles.UseVisualStyleBackColor = true;
			// 
			// listViewTimeSteps
			// 
			this.listViewTimeSteps.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewTimeSteps.CheckBoxes = true;
			this.listViewTimeSteps.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderDummy});
			this.listViewTimeSteps.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listViewTimeSteps.Location = new System.Drawing.Point(12, 25);
			this.listViewTimeSteps.Name = "listViewTimeSteps";
			this.listViewTimeSteps.Size = new System.Drawing.Size(182, 311);
			this.listViewTimeSteps.TabIndex = 18;
			this.listViewTimeSteps.UseCompatibleStateImageBehavior = false;
			this.listViewTimeSteps.View = System.Windows.Forms.View.Details;
			// 
			// columnHeaderDummy
			// 
			this.columnHeaderDummy.Text = "";
			this.columnHeaderDummy.Width = 150;
			// 
			// AnimationCreatorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(286, 436);
			this.Controls.Add(this.listViewTimeSteps);
			this.Controls.Add(this.checkBoxSaveToFiles);
			this.Controls.Add(this.comboBoxStep);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.checkBoxRepeat);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.textBoxFPS);
			this.Controls.Add(this.buttonCreateAnimation);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "AnimationCreatorForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Animation Creator";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonCreateAnimation;
		private System.Windows.Forms.TextBox textBoxFPS;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.CheckBox checkBoxRepeat;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBoxStep;
		private System.Windows.Forms.CheckBox checkBoxSaveToFiles;
		private System.Windows.Forms.ListView listViewTimeSteps;
		private System.Windows.Forms.ColumnHeader columnHeaderDummy;
	}
}