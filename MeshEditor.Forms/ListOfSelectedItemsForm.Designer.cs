namespace MeshEditor.WinUI
{
	partial class ListOfSelectedItemsForm
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
			this.components = new System.ComponentModel.Container();
			this.buttonClose = new System.Windows.Forms.Button();
			this.richTextBox = new System.Windows.Forms.RichTextBox();
			this.textBoxContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.copySelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.selectallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.labelItems = new System.Windows.Forms.Label();
			this.labelShowListOf = new System.Windows.Forms.Label();
			this.comboBoxEntityType = new System.Windows.Forms.ComboBox();
			this.checkBoxShowCompleteInfo = new System.Windows.Forms.CheckBox();
			this.linkLabelAddProperty = new System.Windows.Forms.LinkLabel();
			this.linkLabelRemoveProperty = new System.Windows.Forms.LinkLabel();
			this.textBoxContextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Location = new System.Drawing.Point(475, 447);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(87, 33);
			this.buttonClose.TabIndex = 0;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
			// 
			// richTextBox
			// 
			this.richTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.richTextBox.ContextMenuStrip = this.textBoxContextMenuStrip;
			this.richTextBox.HideSelection = false;
			this.richTextBox.Location = new System.Drawing.Point(12, 103);
			this.richTextBox.Name = "richTextBox";
			this.richTextBox.ReadOnly = true;
			this.richTextBox.Size = new System.Drawing.Size(550, 338);
			this.richTextBox.TabIndex = 3;
			this.richTextBox.Text = "";
			// 
			// textBoxContextMenuStrip
			// 
			this.textBoxContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copySelectionToolStripMenuItem,
            this.copyAllToolStripMenuItem,
            this.toolStripSeparator1,
            this.selectallToolStripMenuItem});
			this.textBoxContextMenuStrip.Name = "textBoxContextMenuStrip";
			this.textBoxContextMenuStrip.Size = new System.Drawing.Size(121, 76);
			this.textBoxContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.textBoxContextMenuStrip_Opening);
			// 
			// copySelectionToolStripMenuItem
			// 
			this.copySelectionToolStripMenuItem.Name = "copySelectionToolStripMenuItem";
			this.copySelectionToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
			this.copySelectionToolStripMenuItem.Text = "&Copy";
			this.copySelectionToolStripMenuItem.Click += new System.EventHandler(this.copyselectionToolStripMenuItem_Click);
			// 
			// copyAllToolStripMenuItem
			// 
			this.copyAllToolStripMenuItem.Name = "copyAllToolStripMenuItem";
			this.copyAllToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
			this.copyAllToolStripMenuItem.Text = "C&opy all";
			this.copyAllToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(117, 6);
			// 
			// selectallToolStripMenuItem
			// 
			this.selectallToolStripMenuItem.Name = "selectallToolStripMenuItem";
			this.selectallToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
			this.selectallToolStripMenuItem.Text = "Select &all";
			this.selectallToolStripMenuItem.Click += new System.EventHandler(this.selectallToolStripMenuItem_Click);
			// 
			// labelItems
			// 
			this.labelItems.AutoSize = true;
			this.labelItems.Location = new System.Drawing.Point(13, 44);
			this.labelItems.Name = "labelItems";
			this.labelItems.Size = new System.Drawing.Size(93, 13);
			this.labelItems.TabIndex = 4;
			this.labelItems.Text = "(Nothing selected)";
			// 
			// labelShowListOf
			// 
			this.labelShowListOf.AutoSize = true;
			this.labelShowListOf.Location = new System.Drawing.Point(12, 20);
			this.labelShowListOf.Name = "labelShowListOf";
			this.labelShowListOf.Size = new System.Drawing.Size(104, 13);
			this.labelShowListOf.TabIndex = 5;
			this.labelShowListOf.Text = "Show list of selected";
			// 
			// comboBoxEntityType
			// 
			this.comboBoxEntityType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxEntityType.FormattingEnabled = true;
			this.comboBoxEntityType.Items.AddRange(new object[] {
            "Nodes",
            "Elements",
            "Faces",
            "Edges"});
			this.comboBoxEntityType.Location = new System.Drawing.Point(171, 18);
			this.comboBoxEntityType.Name = "comboBoxEntityType";
			this.comboBoxEntityType.Size = new System.Drawing.Size(103, 21);
			this.comboBoxEntityType.TabIndex = 6;
			this.comboBoxEntityType.SelectedIndexChanged += new System.EventHandler(this.comboBoxEntityType_SelectedIndexChanged);
			// 
			// checkBoxShowCompleteInfo
			// 
			this.checkBoxShowCompleteInfo.AutoSize = true;
			this.checkBoxShowCompleteInfo.Checked = true;
			this.checkBoxShowCompleteInfo.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxShowCompleteInfo.Location = new System.Drawing.Point(12, 76);
			this.checkBoxShowCompleteInfo.Name = "checkBoxShowCompleteInfo";
			this.checkBoxShowCompleteInfo.Size = new System.Drawing.Size(119, 17);
			this.checkBoxShowCompleteInfo.TabIndex = 9;
			this.checkBoxShowCompleteInfo.Text = "Show complete info";
			this.checkBoxShowCompleteInfo.UseVisualStyleBackColor = true;
			this.checkBoxShowCompleteInfo.CheckedChanged += new System.EventHandler(this.checkBoxShowProperties_CheckedChanged);
			// 
			// linkLabelAddProperty
			// 
			this.linkLabelAddProperty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.linkLabelAddProperty.AutoSize = true;
			this.linkLabelAddProperty.Location = new System.Drawing.Point(354, 20);
			this.linkLabelAddProperty.Name = "linkLabelAddProperty";
			this.linkLabelAddProperty.Size = new System.Drawing.Size(111, 13);
			this.linkLabelAddProperty.TabIndex = 10;
			this.linkLabelAddProperty.TabStop = true;
			this.linkLabelAddProperty.Text = "Add property to nodes";
			this.linkLabelAddProperty.Visible = false;
			this.linkLabelAddProperty.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelAddProperty_LinkClicked);
			// 
			// linkLabelRemoveProperty
			// 
			this.linkLabelRemoveProperty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.linkLabelRemoveProperty.AutoSize = true;
			this.linkLabelRemoveProperty.Location = new System.Drawing.Point(354, 44);
			this.linkLabelRemoveProperty.Name = "linkLabelRemoveProperty";
			this.linkLabelRemoveProperty.Size = new System.Drawing.Size(143, 13);
			this.linkLabelRemoveProperty.TabIndex = 11;
			this.linkLabelRemoveProperty.TabStop = true;
			this.linkLabelRemoveProperty.Text = "Remove property from nodes";
			this.linkLabelRemoveProperty.Visible = false;
			this.linkLabelRemoveProperty.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelRemoveProperty_LinkClicked);
			// 
			// ListOfSelectedItemsForm
			// 
			this.AcceptButton = this.buttonClose;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(574, 492);
			this.Controls.Add(this.linkLabelRemoveProperty);
			this.Controls.Add(this.linkLabelAddProperty);
			this.Controls.Add(this.checkBoxShowCompleteInfo);
			this.Controls.Add(this.richTextBox);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.labelItems);
			this.Controls.Add(this.labelShowListOf);
			this.Controls.Add(this.comboBoxEntityType);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(522, 248);
			this.Name = "ListOfSelectedItemsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "List of selected items";
			this.textBoxContextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.RichTextBox richTextBox;
		private System.Windows.Forms.Label labelItems;
		private System.Windows.Forms.Label labelShowListOf;
		private System.Windows.Forms.ComboBox comboBoxEntityType;
		private System.Windows.Forms.ContextMenuStrip textBoxContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem selectallToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem copySelectionToolStripMenuItem;
		private System.Windows.Forms.CheckBox checkBoxShowCompleteInfo;
		private System.Windows.Forms.LinkLabel linkLabelAddProperty;
		private System.Windows.Forms.LinkLabel linkLabelRemoveProperty;
	}
}