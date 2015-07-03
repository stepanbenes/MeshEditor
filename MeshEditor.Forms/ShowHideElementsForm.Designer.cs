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
			this.label1 = new System.Windows.Forms.Label();
			this.buttonSelectNoneProperties = new System.Windows.Forms.Button();
			this.buttonSelectAllProperties = new System.Windows.Forms.Button();
			this.listViewProperties = new System.Windows.Forms.ListView();
			this.tabPageElementTypes = new System.Windows.Forms.TabPage();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonSelectNoneElementTypes = new System.Windows.Forms.Button();
			this.buttonSelectAllElementTypes = new System.Windows.Forms.Button();
			this.listViewElementTypes = new System.Windows.Forms.ListView();
			this.buttonClose = new System.Windows.Forms.Button();
			this.buttonApply = new System.Windows.Forms.Button();
			this.tabControl.SuspendLayout();
			this.tabPagePropertyNumbers.SuspendLayout();
			this.tabPageElementTypes.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPagePropertyNumbers);
			this.tabControl.Controls.Add(this.tabPageElementTypes);
			this.tabControl.Location = new System.Drawing.Point(12, 12);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(464, 387);
			this.tabControl.TabIndex = 0;
			// 
			// tabPagePropertyNumbers
			// 
			this.tabPagePropertyNumbers.Controls.Add(this.label1);
			this.tabPagePropertyNumbers.Controls.Add(this.buttonSelectNoneProperties);
			this.tabPagePropertyNumbers.Controls.Add(this.buttonSelectAllProperties);
			this.tabPagePropertyNumbers.Controls.Add(this.listViewProperties);
			this.tabPagePropertyNumbers.Location = new System.Drawing.Point(4, 25);
			this.tabPagePropertyNumbers.Name = "tabPagePropertyNumbers";
			this.tabPagePropertyNumbers.Padding = new System.Windows.Forms.Padding(3);
			this.tabPagePropertyNumbers.Size = new System.Drawing.Size(456, 358);
			this.tabPagePropertyNumbers.TabIndex = 0;
			this.tabPagePropertyNumbers.Text = "Property numbers";
			this.tabPagePropertyNumbers.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(187, 17);
			this.label1.TabIndex = 6;
			this.label1.Text = "Shown elements by property";
			// 
			// buttonSelectNoneProperties
			// 
			this.buttonSelectNoneProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonSelectNoneProperties.Location = new System.Drawing.Point(353, 11);
			this.buttonSelectNoneProperties.Name = "buttonSelectNoneProperties";
			this.buttonSelectNoneProperties.Size = new System.Drawing.Size(97, 23);
			this.buttonSelectNoneProperties.TabIndex = 5;
			this.buttonSelectNoneProperties.Text = "Select none";
			this.buttonSelectNoneProperties.UseVisualStyleBackColor = true;
			this.buttonSelectNoneProperties.Click += new System.EventHandler(this.buttonSelectNoneProperties_Click);
			// 
			// buttonSelectAllProperties
			// 
			this.buttonSelectAllProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonSelectAllProperties.Location = new System.Drawing.Point(250, 11);
			this.buttonSelectAllProperties.Name = "buttonSelectAllProperties";
			this.buttonSelectAllProperties.Size = new System.Drawing.Size(97, 23);
			this.buttonSelectAllProperties.TabIndex = 4;
			this.buttonSelectAllProperties.Text = "Select all";
			this.buttonSelectAllProperties.UseVisualStyleBackColor = true;
			this.buttonSelectAllProperties.Click += new System.EventHandler(this.buttonSelectAllProperties_Click);
			// 
			// listViewProperties
			// 
			this.listViewProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.listViewProperties.CheckBoxes = true;
			this.listViewProperties.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.listViewProperties.FullRowSelect = true;
			this.listViewProperties.GridLines = true;
			this.listViewProperties.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listViewProperties.Location = new System.Drawing.Point(6, 40);
			this.listViewProperties.Name = "listViewProperties";
			this.listViewProperties.Size = new System.Drawing.Size(444, 312);
			this.listViewProperties.TabIndex = 3;
			this.listViewProperties.UseCompatibleStateImageBehavior = false;
			this.listViewProperties.View = System.Windows.Forms.View.Details;
			this.listViewProperties.SelectedIndexChanged += new System.EventHandler(this.listViewProperties_SelectedIndexChanged);
			// 
			// tabPageElementTypes
			// 
			this.tabPageElementTypes.Controls.Add(this.label2);
			this.tabPageElementTypes.Controls.Add(this.buttonSelectNoneElementTypes);
			this.tabPageElementTypes.Controls.Add(this.buttonSelectAllElementTypes);
			this.tabPageElementTypes.Controls.Add(this.listViewElementTypes);
			this.tabPageElementTypes.Location = new System.Drawing.Point(4, 25);
			this.tabPageElementTypes.Name = "tabPageElementTypes";
			this.tabPageElementTypes.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageElementTypes.Size = new System.Drawing.Size(456, 358);
			this.tabPageElementTypes.TabIndex = 1;
			this.tabPageElementTypes.Text = "Element types";
			this.tabPageElementTypes.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 14);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(161, 17);
			this.label2.TabIndex = 9;
			this.label2.Text = "Shown elements by type";
			// 
			// buttonSelectNoneElementTypes
			// 
			this.buttonSelectNoneElementTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonSelectNoneElementTypes.Location = new System.Drawing.Point(353, 11);
			this.buttonSelectNoneElementTypes.Name = "buttonSelectNoneElementTypes";
			this.buttonSelectNoneElementTypes.Size = new System.Drawing.Size(97, 23);
			this.buttonSelectNoneElementTypes.TabIndex = 8;
			this.buttonSelectNoneElementTypes.Text = "Select none";
			this.buttonSelectNoneElementTypes.UseVisualStyleBackColor = true;
			this.buttonSelectNoneElementTypes.Click += new System.EventHandler(this.buttonSelectNoneElementTypes_Click);
			// 
			// buttonSelectAllElementTypes
			// 
			this.buttonSelectAllElementTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonSelectAllElementTypes.Location = new System.Drawing.Point(250, 11);
			this.buttonSelectAllElementTypes.Name = "buttonSelectAllElementTypes";
			this.buttonSelectAllElementTypes.Size = new System.Drawing.Size(97, 23);
			this.buttonSelectAllElementTypes.TabIndex = 7;
			this.buttonSelectAllElementTypes.Text = "Select all";
			this.buttonSelectAllElementTypes.UseVisualStyleBackColor = true;
			this.buttonSelectAllElementTypes.Click += new System.EventHandler(this.buttonSelectAllElementTypes_Click);
			// 
			// listViewElementTypes
			// 
			this.listViewElementTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.listViewElementTypes.CheckBoxes = true;
			this.listViewElementTypes.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.listViewElementTypes.FullRowSelect = true;
			this.listViewElementTypes.GridLines = true;
			this.listViewElementTypes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listViewElementTypes.Location = new System.Drawing.Point(6, 40);
			this.listViewElementTypes.Name = "listViewElementTypes";
			this.listViewElementTypes.Size = new System.Drawing.Size(444, 312);
			this.listViewElementTypes.TabIndex = 6;
			this.listViewElementTypes.UseCompatibleStateImageBehavior = false;
			this.listViewElementTypes.View = System.Windows.Forms.View.Details;
			this.listViewElementTypes.SelectedIndexChanged += new System.EventHandler(this.listViewElementTypes_SelectedIndexChanged);
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Location = new System.Drawing.Point(386, 405);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(90, 33);
			this.buttonClose.TabIndex = 1;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
			// 
			// buttonApply
			// 
			this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonApply.Location = new System.Drawing.Point(290, 405);
			this.buttonApply.Name = "buttonApply";
			this.buttonApply.Size = new System.Drawing.Size(90, 33);
			this.buttonApply.TabIndex = 3;
			this.buttonApply.Text = "Apply";
			this.buttonApply.UseVisualStyleBackColor = true;
			this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
			// 
			// ShowHideElementsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(488, 450);
			this.Controls.Add(this.buttonApply);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.tabControl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(461, 297);
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
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPagePropertyNumbers;
		private System.Windows.Forms.TabPage tabPageElementTypes;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.ListView listViewProperties;
		private System.Windows.Forms.Button buttonSelectNoneProperties;
		private System.Windows.Forms.Button buttonSelectAllProperties;
		private System.Windows.Forms.Button buttonSelectNoneElementTypes;
		private System.Windows.Forms.Button buttonSelectAllElementTypes;
		private System.Windows.Forms.ListView listViewElementTypes;
		private System.Windows.Forms.Button buttonApply;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
	}
}