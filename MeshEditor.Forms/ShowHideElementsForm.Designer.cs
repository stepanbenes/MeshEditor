namespace MeshEditor.WinUI
{
	partial class ShowHideElementsForm
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
			this.tabPagePropertyNumbers = new System.Windows.Forms.TabPage();
			this.checkBoxCheckSelectedProperties = new System.Windows.Forms.CheckBox();
			this.checkBoxCheckAllProperties = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.listViewProperties = new System.Windows.Forms.ListView();
			this.columnProperty = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.tabPageElementTypes = new System.Windows.Forms.TabPage();
			this.checkBoxCheckSelectedElementTypes = new System.Windows.Forms.CheckBox();
			this.checkBoxCheckAllElementTypes = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.listViewElementTypes = new System.Windows.Forms.ListView();
			this.columnElementTypes = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.tabPageValueLimit = new System.Windows.Forms.TabPage();
			this.checkBoxInverse = new System.Windows.Forms.CheckBox();
			this.checkBoxAllNodesInRange = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.textBoxMaximum = new System.Windows.Forms.TextBox();
			this.textBoxMinimum = new System.Windows.Forms.TextBox();
			this.checkBoxMaximum = new System.Windows.Forms.CheckBox();
			this.checkBoxMinimum = new System.Windows.Forms.CheckBox();
			this.buttonClose = new System.Windows.Forms.Button();
			this.buttonApply = new System.Windows.Forms.Button();
			this.tabControl.SuspendLayout();
			this.tabPagePropertyNumbers.SuspendLayout();
			this.tabPageElementTypes.SuspendLayout();
			this.tabPageValueLimit.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPagePropertyNumbers);
			this.tabControl.Controls.Add(this.tabPageElementTypes);
			this.tabControl.Controls.Add(this.tabPageValueLimit);
			this.tabControl.Location = new System.Drawing.Point(9, 10);
			this.tabControl.Margin = new System.Windows.Forms.Padding(2);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(409, 443);
			this.tabControl.TabIndex = 0;
			// 
			// tabPagePropertyNumbers
			// 
			this.tabPagePropertyNumbers.Controls.Add(this.checkBoxCheckSelectedProperties);
			this.tabPagePropertyNumbers.Controls.Add(this.checkBoxCheckAllProperties);
			this.tabPagePropertyNumbers.Controls.Add(this.label1);
			this.tabPagePropertyNumbers.Controls.Add(this.listViewProperties);
			this.tabPagePropertyNumbers.Location = new System.Drawing.Point(4, 22);
			this.tabPagePropertyNumbers.Margin = new System.Windows.Forms.Padding(2);
			this.tabPagePropertyNumbers.Name = "tabPagePropertyNumbers";
			this.tabPagePropertyNumbers.Padding = new System.Windows.Forms.Padding(2);
			this.tabPagePropertyNumbers.Size = new System.Drawing.Size(401, 417);
			this.tabPagePropertyNumbers.TabIndex = 0;
			this.tabPagePropertyNumbers.Text = "Property numbers";
			this.tabPagePropertyNumbers.UseVisualStyleBackColor = true;
			// 
			// checkBoxCheckSelectedProperties
			// 
			this.checkBoxCheckSelectedProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxCheckSelectedProperties.AutoSize = true;
			this.checkBoxCheckSelectedProperties.Location = new System.Drawing.Point(91, 395);
			this.checkBoxCheckSelectedProperties.Name = "checkBoxCheckSelectedProperties";
			this.checkBoxCheckSelectedProperties.Size = new System.Drawing.Size(102, 17);
			this.checkBoxCheckSelectedProperties.TabIndex = 8;
			this.checkBoxCheckSelectedProperties.Text = "Check Selected";
			this.checkBoxCheckSelectedProperties.UseVisualStyleBackColor = true;
			this.checkBoxCheckSelectedProperties.CheckedChanged += new System.EventHandler(this.checkBoxCheckSelectedProperties_CheckedChanged);
			// 
			// checkBoxCheckAllProperties
			// 
			this.checkBoxCheckAllProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxCheckAllProperties.AutoSize = true;
			this.checkBoxCheckAllProperties.Location = new System.Drawing.Point(5, 395);
			this.checkBoxCheckAllProperties.Name = "checkBoxCheckAllProperties";
			this.checkBoxCheckAllProperties.Size = new System.Drawing.Size(71, 17);
			this.checkBoxCheckAllProperties.TabIndex = 7;
			this.checkBoxCheckAllProperties.Text = "Check All";
			this.checkBoxCheckAllProperties.UseVisualStyleBackColor = true;
			this.checkBoxCheckAllProperties.CheckedChanged += new System.EventHandler(this.checkBoxCheckAllProperties_CheckedChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(4, 11);
			this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(142, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "Show elements with property";
			// 
			// listViewProperties
			// 
			this.listViewProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewProperties.CheckBoxes = true;
			this.listViewProperties.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnProperty,
            this.columnDescription});
			this.listViewProperties.FullRowSelect = true;
			this.listViewProperties.GridLines = true;
			this.listViewProperties.HideSelection = false;
			this.listViewProperties.Location = new System.Drawing.Point(4, 32);
			this.listViewProperties.Margin = new System.Windows.Forms.Padding(2);
			this.listViewProperties.Name = "listViewProperties";
			this.listViewProperties.Size = new System.Drawing.Size(395, 358);
			this.listViewProperties.TabIndex = 3;
			this.listViewProperties.UseCompatibleStateImageBehavior = false;
			this.listViewProperties.View = System.Windows.Forms.View.Details;
			this.listViewProperties.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listViewProperties_ItemChecked);
			this.listViewProperties.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewProperties_ItemSelectionChanged);
			// 
			// columnProperty
			// 
			this.columnProperty.Text = "Property";
			this.columnProperty.Width = 100;
			// 
			// columnDescription
			// 
			this.columnDescription.Text = "Description";
			this.columnDescription.Width = 280;
			// 
			// tabPageElementTypes
			// 
			this.tabPageElementTypes.Controls.Add(this.checkBoxCheckSelectedElementTypes);
			this.tabPageElementTypes.Controls.Add(this.checkBoxCheckAllElementTypes);
			this.tabPageElementTypes.Controls.Add(this.label2);
			this.tabPageElementTypes.Controls.Add(this.listViewElementTypes);
			this.tabPageElementTypes.Location = new System.Drawing.Point(4, 22);
			this.tabPageElementTypes.Margin = new System.Windows.Forms.Padding(2);
			this.tabPageElementTypes.Name = "tabPageElementTypes";
			this.tabPageElementTypes.Padding = new System.Windows.Forms.Padding(2);
			this.tabPageElementTypes.Size = new System.Drawing.Size(401, 417);
			this.tabPageElementTypes.TabIndex = 1;
			this.tabPageElementTypes.Text = "Element types";
			this.tabPageElementTypes.UseVisualStyleBackColor = true;
			// 
			// checkBoxCheckSelectedElementTypes
			// 
			this.checkBoxCheckSelectedElementTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxCheckSelectedElementTypes.AutoSize = true;
			this.checkBoxCheckSelectedElementTypes.Location = new System.Drawing.Point(91, 395);
			this.checkBoxCheckSelectedElementTypes.Name = "checkBoxCheckSelectedElementTypes";
			this.checkBoxCheckSelectedElementTypes.Size = new System.Drawing.Size(102, 17);
			this.checkBoxCheckSelectedElementTypes.TabIndex = 11;
			this.checkBoxCheckSelectedElementTypes.Text = "Check Selected";
			this.checkBoxCheckSelectedElementTypes.UseVisualStyleBackColor = true;
			this.checkBoxCheckSelectedElementTypes.CheckedChanged += new System.EventHandler(this.checkBoxCheckSelectedElementTypes_CheckedChanged);
			// 
			// checkBoxCheckAllElementTypes
			// 
			this.checkBoxCheckAllElementTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxCheckAllElementTypes.AutoSize = true;
			this.checkBoxCheckAllElementTypes.Location = new System.Drawing.Point(5, 395);
			this.checkBoxCheckAllElementTypes.Name = "checkBoxCheckAllElementTypes";
			this.checkBoxCheckAllElementTypes.Size = new System.Drawing.Size(71, 17);
			this.checkBoxCheckAllElementTypes.TabIndex = 10;
			this.checkBoxCheckAllElementTypes.Text = "Check All";
			this.checkBoxCheckAllElementTypes.UseVisualStyleBackColor = true;
			this.checkBoxCheckAllElementTypes.CheckedChanged += new System.EventHandler(this.checkBoxCheckAllElementTypes_CheckedChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(4, 11);
			this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(114, 13);
			this.label2.TabIndex = 9;
			this.label2.Text = "Show elements of type";
			// 
			// listViewElementTypes
			// 
			this.listViewElementTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewElementTypes.CheckBoxes = true;
			this.listViewElementTypes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnElementTypes});
			this.listViewElementTypes.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.listViewElementTypes.FullRowSelect = true;
			this.listViewElementTypes.GridLines = true;
			this.listViewElementTypes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listViewElementTypes.HideSelection = false;
			this.listViewElementTypes.Location = new System.Drawing.Point(4, 32);
			this.listViewElementTypes.Margin = new System.Windows.Forms.Padding(2);
			this.listViewElementTypes.Name = "listViewElementTypes";
			this.listViewElementTypes.Size = new System.Drawing.Size(395, 358);
			this.listViewElementTypes.TabIndex = 6;
			this.listViewElementTypes.UseCompatibleStateImageBehavior = false;
			this.listViewElementTypes.View = System.Windows.Forms.View.Details;
			this.listViewElementTypes.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listViewElementTypes_ItemChecked);
			this.listViewElementTypes.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewElementTypes_ItemSelectionChanged);
			// 
			// columnElementTypes
			// 
			this.columnElementTypes.Text = "Element types";
			this.columnElementTypes.Width = 200;
			// 
			// tabPageValueLimit
			// 
			this.tabPageValueLimit.Controls.Add(this.checkBoxInverse);
			this.tabPageValueLimit.Controls.Add(this.checkBoxAllNodesInRange);
			this.tabPageValueLimit.Controls.Add(this.label3);
			this.tabPageValueLimit.Controls.Add(this.textBoxMaximum);
			this.tabPageValueLimit.Controls.Add(this.textBoxMinimum);
			this.tabPageValueLimit.Controls.Add(this.checkBoxMaximum);
			this.tabPageValueLimit.Controls.Add(this.checkBoxMinimum);
			this.tabPageValueLimit.Location = new System.Drawing.Point(4, 22);
			this.tabPageValueLimit.Name = "tabPageValueLimit";
			this.tabPageValueLimit.Size = new System.Drawing.Size(401, 417);
			this.tabPageValueLimit.TabIndex = 2;
			this.tabPageValueLimit.Text = "Value limit";
			this.tabPageValueLimit.UseVisualStyleBackColor = true;
			// 
			// checkBoxInverse
			// 
			this.checkBoxInverse.AutoSize = true;
			this.checkBoxInverse.Enabled = false;
			this.checkBoxInverse.Location = new System.Drawing.Point(7, 96);
			this.checkBoxInverse.Name = "checkBoxInverse";
			this.checkBoxInverse.Size = new System.Drawing.Size(61, 17);
			this.checkBoxInverse.TabIndex = 6;
			this.checkBoxInverse.Text = "Inverse";
			this.checkBoxInverse.UseVisualStyleBackColor = true;
			// 
			// checkBoxAllNodesInRange
			// 
			this.checkBoxAllNodesInRange.AutoSize = true;
			this.checkBoxAllNodesInRange.Enabled = false;
			this.checkBoxAllNodesInRange.Location = new System.Drawing.Point(7, 119);
			this.checkBoxAllNodesInRange.Name = "checkBoxAllNodesInRange";
			this.checkBoxAllNodesInRange.Size = new System.Drawing.Size(162, 17);
			this.checkBoxAllNodesInRange.TabIndex = 5;
			this.checkBoxAllNodesInRange.Text = "All nodes of element in range";
			this.checkBoxAllNodesInRange.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(4, 11);
			this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(227, 13);
			this.label3.TabIndex = 4;
			this.label3.Text = "Show elements with nodes in data value range";
			// 
			// textBoxMaximum
			// 
			this.textBoxMaximum.Enabled = false;
			this.textBoxMaximum.Location = new System.Drawing.Point(83, 34);
			this.textBoxMaximum.Name = "textBoxMaximum";
			this.textBoxMaximum.Size = new System.Drawing.Size(100, 20);
			this.textBoxMaximum.TabIndex = 3;
			this.textBoxMaximum.Text = "0";
			// 
			// textBoxMinimum
			// 
			this.textBoxMinimum.Enabled = false;
			this.textBoxMinimum.Location = new System.Drawing.Point(83, 57);
			this.textBoxMinimum.Name = "textBoxMinimum";
			this.textBoxMinimum.Size = new System.Drawing.Size(100, 20);
			this.textBoxMinimum.TabIndex = 2;
			this.textBoxMinimum.Text = "0";
			// 
			// checkBoxMaximum
			// 
			this.checkBoxMaximum.AutoSize = true;
			this.checkBoxMaximum.Location = new System.Drawing.Point(7, 36);
			this.checkBoxMaximum.Name = "checkBoxMaximum";
			this.checkBoxMaximum.Size = new System.Drawing.Size(70, 17);
			this.checkBoxMaximum.TabIndex = 1;
			this.checkBoxMaximum.Text = "Maximum";
			this.checkBoxMaximum.UseVisualStyleBackColor = true;
			this.checkBoxMaximum.CheckedChanged += new System.EventHandler(this.checkBoxMaximum_CheckedChanged);
			// 
			// checkBoxMinimum
			// 
			this.checkBoxMinimum.AutoSize = true;
			this.checkBoxMinimum.Location = new System.Drawing.Point(7, 59);
			this.checkBoxMinimum.Name = "checkBoxMinimum";
			this.checkBoxMinimum.Size = new System.Drawing.Size(67, 17);
			this.checkBoxMinimum.TabIndex = 0;
			this.checkBoxMinimum.Text = "Minimum";
			this.checkBoxMinimum.UseVisualStyleBackColor = true;
			this.checkBoxMinimum.CheckedChanged += new System.EventHandler(this.checkBoxMinimum_CheckedChanged);
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Location = new System.Drawing.Point(351, 458);
			this.buttonClose.Margin = new System.Windows.Forms.Padding(2);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(68, 27);
			this.buttonClose.TabIndex = 1;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
			// 
			// buttonApply
			// 
			this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonApply.Location = new System.Drawing.Point(279, 458);
			this.buttonApply.Margin = new System.Windows.Forms.Padding(2);
			this.buttonApply.Name = "buttonApply";
			this.buttonApply.Size = new System.Drawing.Size(68, 27);
			this.buttonApply.TabIndex = 3;
			this.buttonApply.Text = "Apply";
			this.buttonApply.UseVisualStyleBackColor = true;
			this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
			// 
			// ShowHideElementsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(427, 495);
			this.Controls.Add(this.buttonApply);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.tabControl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(2);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(350, 249);
			this.Name = "ShowHideElementsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Show / Hide elements";
			this.TopMost = true;
			this.tabControl.ResumeLayout(false);
			this.tabPagePropertyNumbers.ResumeLayout(false);
			this.tabPagePropertyNumbers.PerformLayout();
			this.tabPageElementTypes.ResumeLayout(false);
			this.tabPageElementTypes.PerformLayout();
			this.tabPageValueLimit.ResumeLayout(false);
			this.tabPageValueLimit.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPagePropertyNumbers;
		private System.Windows.Forms.TabPage tabPageElementTypes;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.ListView listViewProperties;
		private System.Windows.Forms.ListView listViewElementTypes;
		private System.Windows.Forms.Button buttonApply;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TabPage tabPageValueLimit;
		private System.Windows.Forms.TextBox textBoxMaximum;
		private System.Windows.Forms.TextBox textBoxMinimum;
		private System.Windows.Forms.CheckBox checkBoxMaximum;
		private System.Windows.Forms.CheckBox checkBoxMinimum;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckBox checkBoxAllNodesInRange;
		private System.Windows.Forms.CheckBox checkBoxInverse;
		private System.Windows.Forms.ColumnHeader columnProperty;
		private System.Windows.Forms.ColumnHeader columnDescription;
		private System.Windows.Forms.ColumnHeader columnElementTypes;
		private System.Windows.Forms.CheckBox checkBoxCheckAllProperties;
		private System.Windows.Forms.CheckBox checkBoxCheckSelectedProperties;
		private System.Windows.Forms.CheckBox checkBoxCheckAllElementTypes;
		private System.Windows.Forms.CheckBox checkBoxCheckSelectedElementTypes;
	}
}