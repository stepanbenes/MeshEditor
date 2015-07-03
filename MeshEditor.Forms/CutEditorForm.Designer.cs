namespace MeshEditor.WinUI
{
	partial class CutEditorForm
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
			this.tabPageCuttingPlanes = new System.Windows.Forms.TabPage();
			this.labelCutType = new System.Windows.Forms.Label();
			this.comboBoxCutType = new System.Windows.Forms.ComboBox();
			this.buttonInsertNextPoint = new System.Windows.Forms.Button();
			this.buttonInvertSelectedPlanes = new System.Windows.Forms.Button();
			this.listBoxCutPlanes = new System.Windows.Forms.ListBox();
			this.buttonDeleteSelectedPlanes = new System.Windows.Forms.Button();
			this.buttonCreateNewCutPlane = new System.Windows.Forms.Button();
			this.tabPageExpression = new System.Windows.Forms.TabPage();
			this.pictureBoxHelp = new System.Windows.Forms.PictureBox();
			this.labelExpression = new System.Windows.Forms.Label();
			this.textBoxExpression = new System.Windows.Forms.TextBox();
			this.labelAction = new System.Windows.Forms.Label();
			this.comboBoxAction = new System.Windows.Forms.ComboBox();
			this.buttonDoIt = new System.Windows.Forms.Button();
			this.buttonClose = new System.Windows.Forms.Button();
			this.checkBoxFullEntityMatch = new System.Windows.Forms.CheckBox();
			this.buttonRestoreMesh = new System.Windows.Forms.Button();
			this.tabControl.SuspendLayout();
			this.tabPageCuttingPlanes.SuspendLayout();
			this.tabPageExpression.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxHelp)).BeginInit();
			this.SuspendLayout();
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPageCuttingPlanes);
			this.tabControl.Controls.Add(this.tabPageExpression);
			this.tabControl.Location = new System.Drawing.Point(12, 12);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(459, 250);
			this.tabControl.TabIndex = 0;
			// 
			// tabPageCuttingPlanes
			// 
			this.tabPageCuttingPlanes.Controls.Add(this.labelCutType);
			this.tabPageCuttingPlanes.Controls.Add(this.comboBoxCutType);
			this.tabPageCuttingPlanes.Controls.Add(this.buttonInsertNextPoint);
			this.tabPageCuttingPlanes.Controls.Add(this.buttonInvertSelectedPlanes);
			this.tabPageCuttingPlanes.Controls.Add(this.listBoxCutPlanes);
			this.tabPageCuttingPlanes.Controls.Add(this.buttonDeleteSelectedPlanes);
			this.tabPageCuttingPlanes.Controls.Add(this.buttonCreateNewCutPlane);
			this.tabPageCuttingPlanes.Location = new System.Drawing.Point(4, 25);
			this.tabPageCuttingPlanes.Name = "tabPageCuttingPlanes";
			this.tabPageCuttingPlanes.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageCuttingPlanes.Size = new System.Drawing.Size(451, 221);
			this.tabPageCuttingPlanes.TabIndex = 0;
			this.tabPageCuttingPlanes.Text = "Cutting planes";
			this.tabPageCuttingPlanes.UseVisualStyleBackColor = true;
			// 
			// labelCutType
			// 
			this.labelCutType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelCutType.AutoSize = true;
			this.labelCutType.Location = new System.Drawing.Point(343, 123);
			this.labelCutType.Name = "labelCutType";
			this.labelCutType.Size = new System.Drawing.Size(64, 17);
			this.labelCutType.TabIndex = 6;
			this.labelCutType.Text = "Cut type:";
			this.labelCutType.Visible = false;
			// 
			// comboBoxCutType
			// 
			this.comboBoxCutType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.comboBoxCutType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxCutType.FormattingEnabled = true;
			this.comboBoxCutType.Location = new System.Drawing.Point(343, 143);
			this.comboBoxCutType.Name = "comboBoxCutType";
			this.comboBoxCutType.Size = new System.Drawing.Size(102, 24);
			this.comboBoxCutType.TabIndex = 5;
			this.comboBoxCutType.Visible = false;
			// 
			// buttonInsertNextPoint
			// 
			this.buttonInsertNextPoint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonInsertNextPoint.Location = new System.Drawing.Point(176, 135);
			this.buttonInsertNextPoint.Name = "buttonInsertNextPoint";
			this.buttonInsertNextPoint.Size = new System.Drawing.Size(161, 32);
			this.buttonInsertNextPoint.TabIndex = 4;
			this.buttonInsertNextPoint.Text = "Insert next point";
			this.buttonInsertNextPoint.UseVisualStyleBackColor = true;
			this.buttonInsertNextPoint.Visible = false;
			this.buttonInsertNextPoint.Click += new System.EventHandler(this.buttonInsertNextPoint_Click);
			// 
			// buttonInvertSelectedPlanes
			// 
			this.buttonInvertSelectedPlanes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonInvertSelectedPlanes.Location = new System.Drawing.Point(6, 173);
			this.buttonInvertSelectedPlanes.Name = "buttonInvertSelectedPlanes";
			this.buttonInvertSelectedPlanes.Size = new System.Drawing.Size(164, 32);
			this.buttonInvertSelectedPlanes.TabIndex = 3;
			this.buttonInvertSelectedPlanes.Text = "Invert selected planes";
			this.buttonInvertSelectedPlanes.UseVisualStyleBackColor = true;
			this.buttonInvertSelectedPlanes.Visible = false;
			this.buttonInvertSelectedPlanes.Click += new System.EventHandler(this.buttonInvertSelectedPlanes_Click);
			// 
			// listBoxCutPlanes
			// 
			this.listBoxCutPlanes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.listBoxCutPlanes.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.listBoxCutPlanes.FormattingEnabled = true;
			this.listBoxCutPlanes.ItemHeight = 16;
			this.listBoxCutPlanes.Location = new System.Drawing.Point(6, 17);
			this.listBoxCutPlanes.Name = "listBoxCutPlanes";
			this.listBoxCutPlanes.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.listBoxCutPlanes.Size = new System.Drawing.Size(439, 100);
			this.listBoxCutPlanes.TabIndex = 2;
			this.listBoxCutPlanes.SelectedIndexChanged += new System.EventHandler(this.listBoxCutPlanes_SelectedIndexChanged);
			// 
			// buttonDeleteSelectedPlanes
			// 
			this.buttonDeleteSelectedPlanes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonDeleteSelectedPlanes.Location = new System.Drawing.Point(176, 173);
			this.buttonDeleteSelectedPlanes.Name = "buttonDeleteSelectedPlanes";
			this.buttonDeleteSelectedPlanes.Size = new System.Drawing.Size(161, 32);
			this.buttonDeleteSelectedPlanes.TabIndex = 1;
			this.buttonDeleteSelectedPlanes.Text = "Delete selected planes";
			this.buttonDeleteSelectedPlanes.UseVisualStyleBackColor = true;
			this.buttonDeleteSelectedPlanes.Visible = false;
			this.buttonDeleteSelectedPlanes.Click += new System.EventHandler(this.buttonDeleteSelectedPlanes_Click);
			// 
			// buttonCreateNewCutPlane
			// 
			this.buttonCreateNewCutPlane.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonCreateNewCutPlane.Location = new System.Drawing.Point(6, 135);
			this.buttonCreateNewCutPlane.Name = "buttonCreateNewCutPlane";
			this.buttonCreateNewCutPlane.Size = new System.Drawing.Size(164, 32);
			this.buttonCreateNewCutPlane.TabIndex = 0;
			this.buttonCreateNewCutPlane.Text = "Create new cut plane";
			this.buttonCreateNewCutPlane.UseVisualStyleBackColor = true;
			this.buttonCreateNewCutPlane.Click += new System.EventHandler(this.buttonCreateCutPlane_Click);
			// 
			// tabPageExpression
			// 
			this.tabPageExpression.Controls.Add(this.pictureBoxHelp);
			this.tabPageExpression.Controls.Add(this.labelExpression);
			this.tabPageExpression.Controls.Add(this.textBoxExpression);
			this.tabPageExpression.Location = new System.Drawing.Point(4, 25);
			this.tabPageExpression.Name = "tabPageExpression";
			this.tabPageExpression.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageExpression.Size = new System.Drawing.Size(451, 221);
			this.tabPageExpression.TabIndex = 1;
			this.tabPageExpression.Text = "Expression";
			this.tabPageExpression.UseVisualStyleBackColor = true;
			// 
			// pictureBoxHelp
			// 
			this.pictureBoxHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBoxHelp.Cursor = System.Windows.Forms.Cursors.Hand;
			this.pictureBoxHelp.Image = global::MeshEditor.Forms.Properties.Resources.help32;
			this.pictureBoxHelp.Location = new System.Drawing.Point(413, 114);
			this.pictureBoxHelp.Name = "pictureBoxHelp";
			this.pictureBoxHelp.Size = new System.Drawing.Size(32, 32);
			this.pictureBoxHelp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pictureBoxHelp.TabIndex = 3;
			this.pictureBoxHelp.TabStop = false;
			this.pictureBoxHelp.Click += new System.EventHandler(this.pictureBoxHelp_Click);
			// 
			// labelExpression
			// 
			this.labelExpression.AutoSize = true;
			this.labelExpression.Location = new System.Drawing.Point(6, 61);
			this.labelExpression.Name = "labelExpression";
			this.labelExpression.Size = new System.Drawing.Size(252, 17);
			this.labelExpression.TabIndex = 1;
			this.labelExpression.Text = "Insert equation specifying cutting area:";
			// 
			// textBoxExpression
			// 
			this.textBoxExpression.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxExpression.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.textBoxExpression.Location = new System.Drawing.Point(6, 81);
			this.textBoxExpression.Name = "textBoxExpression";
			this.textBoxExpression.Size = new System.Drawing.Size(439, 27);
			this.textBoxExpression.TabIndex = 0;
			// 
			// labelAction
			// 
			this.labelAction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelAction.AutoSize = true;
			this.labelAction.Location = new System.Drawing.Point(9, 276);
			this.labelAction.Name = "labelAction";
			this.labelAction.Size = new System.Drawing.Size(51, 17);
			this.labelAction.TabIndex = 1;
			this.labelAction.Text = "Action:";
			// 
			// comboBoxAction
			// 
			this.comboBoxAction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.comboBoxAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxAction.FormattingEnabled = true;
			this.comboBoxAction.Items.AddRange(new object[] {
            "Cut",
            "Select elements",
            "Select nodes",
            "Select faces",
            "Select edges",
            "Select beams"});
			this.comboBoxAction.Location = new System.Drawing.Point(66, 276);
			this.comboBoxAction.Name = "comboBoxAction";
			this.comboBoxAction.Size = new System.Drawing.Size(146, 24);
			this.comboBoxAction.TabIndex = 2;
			this.comboBoxAction.SelectedIndexChanged += new System.EventHandler(this.comboBoxAction_SelectedIndexChanged);
			// 
			// buttonDoIt
			// 
			this.buttonDoIt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonDoIt.Location = new System.Drawing.Point(283, 304);
			this.buttonDoIt.Name = "buttonDoIt";
			this.buttonDoIt.Size = new System.Drawing.Size(107, 32);
			this.buttonDoIt.TabIndex = 3;
			this.buttonDoIt.Text = "Cut";
			this.buttonDoIt.UseVisualStyleBackColor = true;
			this.buttonDoIt.Click += new System.EventHandler(this.buttonDoIt_Click);
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClose.Location = new System.Drawing.Point(396, 304);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(75, 32);
			this.buttonClose.TabIndex = 4;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
			// 
			// checkBoxFullEntityMatch
			// 
			this.checkBoxFullEntityMatch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxFullEntityMatch.AutoSize = true;
			this.checkBoxFullEntityMatch.Checked = true;
			this.checkBoxFullEntityMatch.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxFullEntityMatch.Location = new System.Drawing.Point(66, 311);
			this.checkBoxFullEntityMatch.Name = "checkBoxFullEntityMatch";
			this.checkBoxFullEntityMatch.Size = new System.Drawing.Size(132, 21);
			this.checkBoxFullEntityMatch.TabIndex = 5;
			this.checkBoxFullEntityMatch.Text = "Full entity match";
			this.checkBoxFullEntityMatch.UseVisualStyleBackColor = true;
			// 
			// buttonRestoreMesh
			// 
			this.buttonRestoreMesh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonRestoreMesh.Location = new System.Drawing.Point(283, 268);
			this.buttonRestoreMesh.Name = "buttonRestoreMesh";
			this.buttonRestoreMesh.Size = new System.Drawing.Size(107, 32);
			this.buttonRestoreMesh.TabIndex = 6;
			this.buttonRestoreMesh.Text = "Restore mesh";
			this.buttonRestoreMesh.UseVisualStyleBackColor = true;
			this.buttonRestoreMesh.Click += new System.EventHandler(this.buttonRestoreMesh_Click);
			// 
			// CutEditorForm
			// 
			this.AcceptButton = this.buttonDoIt;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(483, 344);
			this.Controls.Add(this.buttonRestoreMesh);
			this.Controls.Add(this.checkBoxFullEntityMatch);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.buttonDoIt);
			this.Controls.Add(this.comboBoxAction);
			this.Controls.Add(this.labelAction);
			this.Controls.Add(this.tabControl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(501, 384);
			this.Name = "CutEditorForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Cutting area editor";
			this.TopMost = true;
			this.tabControl.ResumeLayout(false);
			this.tabPageCuttingPlanes.ResumeLayout(false);
			this.tabPageCuttingPlanes.PerformLayout();
			this.tabPageExpression.ResumeLayout(false);
			this.tabPageExpression.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxHelp)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPageCuttingPlanes;
		private System.Windows.Forms.TabPage tabPageExpression;
		private System.Windows.Forms.Label labelAction;
		private System.Windows.Forms.ComboBox comboBoxAction;
		private System.Windows.Forms.Button buttonDoIt;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.TextBox textBoxExpression;
		private System.Windows.Forms.Button buttonCreateNewCutPlane;
		private System.Windows.Forms.Button buttonDeleteSelectedPlanes;
		private System.Windows.Forms.ListBox listBoxCutPlanes;
		private System.Windows.Forms.Button buttonInvertSelectedPlanes;
		private System.Windows.Forms.Button buttonInsertNextPoint;
		private System.Windows.Forms.Label labelExpression;
		private System.Windows.Forms.Label labelCutType;
		private System.Windows.Forms.ComboBox comboBoxCutType;
		private System.Windows.Forms.CheckBox checkBoxFullEntityMatch;
		private System.Windows.Forms.Button buttonRestoreMesh;
		private System.Windows.Forms.PictureBox pictureBoxHelp;
	}
}