namespace MeshEditor.DataVisualizer.UI
{
	partial class DataVisualizerLoaderForm
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
			this.buttonAddFiles = new System.Windows.Forms.Button();
			this.buttonClose = new System.Windows.Forms.Button();
			this.listViewFiles = new System.Windows.Forms.ListView();
			this.buttonRemove = new System.Windows.Forms.Button();
			this.buttonUnload = new System.Windows.Forms.Button();
			this.comboBoxApproximationMethod = new System.Windows.Forms.ComboBox();
			this.buttonReload = new System.Windows.Forms.Button();
			this.labelApproximationMethodHeader = new System.Windows.Forms.Label();
			this.buttonLoad = new System.Windows.Forms.Button();
			this.labelApproximationQualityText = new System.Windows.Forms.Label();
			this.buttonClear = new System.Windows.Forms.Button();
			this.comboBoxDataVisualizerType = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.checkBoxLoadInternalEntities = new System.Windows.Forms.CheckBox();
			this.textBoxKeyTimeSteps = new System.Windows.Forms.TextBox();
			this.labelKeyTimeSteps = new System.Windows.Forms.Label();
			this.checkBoxCompressTime = new System.Windows.Forms.CheckBox();
			this.linkLabelApproximationQuality = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// buttonAddFiles
			// 
			this.buttonAddFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonAddFiles.Location = new System.Drawing.Point(12, 117);
			this.buttonAddFiles.Name = "buttonAddFiles";
			this.buttonAddFiles.Size = new System.Drawing.Size(75, 23);
			this.buttonAddFiles.TabIndex = 1;
			this.buttonAddFiles.Text = "Add files";
			this.buttonAddFiles.UseVisualStyleBackColor = true;
			this.buttonAddFiles.Click += new System.EventHandler(this.buttonAddFiles_Click);
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Location = new System.Drawing.Point(395, 320);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(75, 23);
			this.buttonClose.TabIndex = 2;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
			// 
			// listViewFiles
			// 
			this.listViewFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewFiles.Location = new System.Drawing.Point(12, 12);
			this.listViewFiles.Name = "listViewFiles";
			this.listViewFiles.Size = new System.Drawing.Size(458, 99);
			this.listViewFiles.TabIndex = 5;
			this.listViewFiles.UseCompatibleStateImageBehavior = false;
			this.listViewFiles.View = System.Windows.Forms.View.List;
			this.listViewFiles.SelectedIndexChanged += new System.EventHandler(this.listViewFiles_SelectedIndexChanged);
			// 
			// buttonRemove
			// 
			this.buttonRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonRemove.Enabled = false;
			this.buttonRemove.Location = new System.Drawing.Point(93, 117);
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.Size = new System.Drawing.Size(75, 23);
			this.buttonRemove.TabIndex = 6;
			this.buttonRemove.Text = "Remove";
			this.buttonRemove.UseVisualStyleBackColor = true;
			this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
			// 
			// buttonUnload
			// 
			this.buttonUnload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonUnload.Location = new System.Drawing.Point(306, 320);
			this.buttonUnload.Name = "buttonUnload";
			this.buttonUnload.Size = new System.Drawing.Size(75, 23);
			this.buttonUnload.TabIndex = 7;
			this.buttonUnload.Text = "Unload";
			this.buttonUnload.UseVisualStyleBackColor = true;
			this.buttonUnload.Click += new System.EventHandler(this.buttonUnload_Click);
			// 
			// comboBoxApproximationMethod
			// 
			this.comboBoxApproximationMethod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.comboBoxApproximationMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxApproximationMethod.FormattingEnabled = true;
			this.comboBoxApproximationMethod.Location = new System.Drawing.Point(174, 164);
			this.comboBoxApproximationMethod.Name = "comboBoxApproximationMethod";
			this.comboBoxApproximationMethod.Size = new System.Drawing.Size(156, 21);
			this.comboBoxApproximationMethod.TabIndex = 8;
			this.comboBoxApproximationMethod.SelectedIndexChanged += new System.EventHandler(this.comboBoxApproximationMethod_SelectedIndexChanged);
			// 
			// buttonReload
			// 
			this.buttonReload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonReload.Location = new System.Drawing.Point(225, 320);
			this.buttonReload.Name = "buttonReload";
			this.buttonReload.Size = new System.Drawing.Size(75, 23);
			this.buttonReload.TabIndex = 9;
			this.buttonReload.Text = "Reload";
			this.buttonReload.UseVisualStyleBackColor = true;
			this.buttonReload.Click += new System.EventHandler(this.buttonReload_Click);
			// 
			// labelApproximationMethodHeader
			// 
			this.labelApproximationMethodHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelApproximationMethodHeader.AutoSize = true;
			this.labelApproximationMethodHeader.Location = new System.Drawing.Point(171, 148);
			this.labelApproximationMethodHeader.Name = "labelApproximationMethodHeader";
			this.labelApproximationMethodHeader.Size = new System.Drawing.Size(111, 13);
			this.labelApproximationMethodHeader.TabIndex = 10;
			this.labelApproximationMethodHeader.Text = "Approximation method";
			// 
			// buttonLoad
			// 
			this.buttonLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonLoad.Location = new System.Drawing.Point(144, 320);
			this.buttonLoad.Name = "buttonLoad";
			this.buttonLoad.Size = new System.Drawing.Size(75, 23);
			this.buttonLoad.TabIndex = 11;
			this.buttonLoad.Text = "Load";
			this.buttonLoad.UseVisualStyleBackColor = true;
			this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
			// 
			// labelApproximationQualityText
			// 
			this.labelApproximationQualityText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.labelApproximationQualityText.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.labelApproximationQualityText.Location = new System.Drawing.Point(9, 258);
			this.labelApproximationQualityText.Name = "labelApproximationQualityText";
			this.labelApproximationQualityText.Size = new System.Drawing.Size(461, 59);
			this.labelApproximationQualityText.TabIndex = 13;
			this.labelApproximationQualityText.Text = "[None]";
			// 
			// buttonClear
			// 
			this.buttonClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonClear.Location = new System.Drawing.Point(174, 117);
			this.buttonClear.Name = "buttonClear";
			this.buttonClear.Size = new System.Drawing.Size(75, 23);
			this.buttonClear.TabIndex = 14;
			this.buttonClear.Text = "Clear";
			this.buttonClear.UseVisualStyleBackColor = true;
			this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
			// 
			// comboBoxDataVisualizerType
			// 
			this.comboBoxDataVisualizerType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.comboBoxDataVisualizerType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDataVisualizerType.FormattingEnabled = true;
			this.comboBoxDataVisualizerType.Location = new System.Drawing.Point(15, 164);
			this.comboBoxDataVisualizerType.Name = "comboBoxDataVisualizerType";
			this.comboBoxDataVisualizerType.Size = new System.Drawing.Size(123, 21);
			this.comboBoxDataVisualizerType.TabIndex = 15;
			this.comboBoxDataVisualizerType.SelectedIndexChanged += new System.EventHandler(this.comboBoxDataVisualizerType_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 148);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(99, 13);
			this.label2.TabIndex = 16;
			this.label2.Text = "Data visualizer type";
			// 
			// checkBoxLoadInternalEntities
			// 
			this.checkBoxLoadInternalEntities.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxLoadInternalEntities.AutoSize = true;
			this.checkBoxLoadInternalEntities.Location = new System.Drawing.Point(15, 196);
			this.checkBoxLoadInternalEntities.Name = "checkBoxLoadInternalEntities";
			this.checkBoxLoadInternalEntities.Size = new System.Drawing.Size(123, 17);
			this.checkBoxLoadInternalEntities.TabIndex = 17;
			this.checkBoxLoadInternalEntities.Text = "Load internal entities";
			this.checkBoxLoadInternalEntities.UseVisualStyleBackColor = true;
			this.checkBoxLoadInternalEntities.CheckedChanged += new System.EventHandler(this.checkBoxLoadInternalEntities_CheckedChanged);
			// 
			// textBoxKeyTimeSteps
			// 
			this.textBoxKeyTimeSteps.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxKeyTimeSteps.Location = new System.Drawing.Point(174, 232);
			this.textBoxKeyTimeSteps.Name = "textBoxKeyTimeSteps";
			this.textBoxKeyTimeSteps.Size = new System.Drawing.Size(296, 20);
			this.textBoxKeyTimeSteps.TabIndex = 18;
			// 
			// labelKeyTimeSteps
			// 
			this.labelKeyTimeSteps.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelKeyTimeSteps.AutoSize = true;
			this.labelKeyTimeSteps.Location = new System.Drawing.Point(171, 216);
			this.labelKeyTimeSteps.Name = "labelKeyTimeSteps";
			this.labelKeyTimeSteps.Size = new System.Drawing.Size(75, 13);
			this.labelKeyTimeSteps.TabIndex = 19;
			this.labelKeyTimeSteps.Text = "Key time steps";
			// 
			// checkBoxCompressTime
			// 
			this.checkBoxCompressTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxCompressTime.AutoSize = true;
			this.checkBoxCompressTime.Location = new System.Drawing.Point(174, 196);
			this.checkBoxCompressTime.Name = "checkBoxCompressTime";
			this.checkBoxCompressTime.Size = new System.Drawing.Size(94, 17);
			this.checkBoxCompressTime.TabIndex = 20;
			this.checkBoxCompressTime.Text = "Compress time";
			this.checkBoxCompressTime.UseVisualStyleBackColor = true;
			this.checkBoxCompressTime.CheckedChanged += new System.EventHandler(this.checkBoxCompressTime_CheckedChanged);
			// 
			// linkLabelApproximationQuality
			// 
			this.linkLabelApproximationQuality.AutoSize = true;
			this.linkLabelApproximationQuality.Location = new System.Drawing.Point(9, 245);
			this.linkLabelApproximationQuality.Name = "linkLabelApproximationQuality";
			this.linkLabelApproximationQuality.Size = new System.Drawing.Size(106, 13);
			this.linkLabelApproximationQuality.TabIndex = 21;
			this.linkLabelApproximationQuality.TabStop = true;
			this.linkLabelApproximationQuality.Text = "Approximation quality";
			this.linkLabelApproximationQuality.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelApproximationQuality_LinkClicked);
			// 
			// DataVisualizerLoaderForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(482, 355);
			this.Controls.Add(this.linkLabelApproximationQuality);
			this.Controls.Add(this.checkBoxCompressTime);
			this.Controls.Add(this.labelKeyTimeSteps);
			this.Controls.Add(this.textBoxKeyTimeSteps);
			this.Controls.Add(this.checkBoxLoadInternalEntities);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.comboBoxDataVisualizerType);
			this.Controls.Add(this.buttonClear);
			this.Controls.Add(this.labelApproximationQualityText);
			this.Controls.Add(this.buttonLoad);
			this.Controls.Add(this.labelApproximationMethodHeader);
			this.Controls.Add(this.buttonReload);
			this.Controls.Add(this.comboBoxApproximationMethod);
			this.Controls.Add(this.buttonUnload);
			this.Controls.Add(this.buttonRemove);
			this.Controls.Add(this.listViewFiles);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.buttonAddFiles);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(400, 350);
			this.Name = "DataVisualizerLoaderForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Data visualizer loader";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonAddFiles;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.ListView listViewFiles;
		private System.Windows.Forms.Button buttonRemove;
		private System.Windows.Forms.Button buttonUnload;
		private System.Windows.Forms.ComboBox comboBoxApproximationMethod;
		private System.Windows.Forms.Button buttonReload;
		private System.Windows.Forms.Label labelApproximationMethodHeader;
		private System.Windows.Forms.Button buttonLoad;
		private System.Windows.Forms.Label labelApproximationQualityText;
		private System.Windows.Forms.Button buttonClear;
		private System.Windows.Forms.ComboBox comboBoxDataVisualizerType;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox checkBoxLoadInternalEntities;
		private System.Windows.Forms.TextBox textBoxKeyTimeSteps;
		private System.Windows.Forms.Label labelKeyTimeSteps;
		private System.Windows.Forms.CheckBox checkBoxCompressTime;
		private System.Windows.Forms.LinkLabel linkLabelApproximationQuality;
	}
}