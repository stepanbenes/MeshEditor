namespace MeshEditor.WinUI
{
	partial class MeshInfoForm
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.buttonApply = new System.Windows.Forms.Button();
			this.secondBorderLimitTrackBar = new System.Windows.Forms.TrackBar();
			this.secondBorderLimitLabel = new System.Windows.Forms.Label();
			this.firstBorderLimitTrackBar = new System.Windows.Forms.TrackBar();
			this.buttonClose = new System.Windows.Forms.Button();
			this.firstBorderLimitLabel = new System.Windows.Forms.Label();
			this.labelNodeCount = new System.Windows.Forms.Label();
			this.labelElementCount = new System.Windows.Forms.Label();
			this.groupBoxBasicInfo = new System.Windows.Forms.GroupBox();
			this.labelBeamCount = new System.Windows.Forms.Label();
			this.labelEdgeCount = new System.Windows.Forms.Label();
			this.labelFaceCount = new System.Windows.Forms.Label();
			this.labelSurfaceItems = new System.Windows.Forms.Label();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabPageBasicInfo = new System.Windows.Forms.TabPage();
			this.tabPagePropertyDescriptions = new System.Windows.Forms.TabPage();
			this.dataGridViewPropertyDescriptions = new System.Windows.Forms.DataGridView();
			this.buttonEditComment = new System.Windows.Forms.Button();
			this.buttonEditPropertyCommands = new System.Windows.Forms.Button();
			this.tabPageEdgeAngleLimits = new System.Windows.Forms.TabPage();
			this.label1 = new System.Windows.Forms.Label();
			this.histogramViewer = new MeshEditor.WinUI.HistogramViewer();
			this.PropertyColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.TargetEntityColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.CommandsColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.CommentColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.secondBorderLimitTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.firstBorderLimitTrackBar)).BeginInit();
			this.groupBoxBasicInfo.SuspendLayout();
			this.tabControl.SuspendLayout();
			this.tabPageBasicInfo.SuspendLayout();
			this.tabPagePropertyDescriptions.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewPropertyDescriptions)).BeginInit();
			this.tabPageEdgeAngleLimits.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonApply
			// 
			this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonApply.Location = new System.Drawing.Point(858, 382);
			this.buttonApply.Margin = new System.Windows.Forms.Padding(2);
			this.buttonApply.Name = "buttonApply";
			this.buttonApply.Size = new System.Drawing.Size(94, 28);
			this.buttonApply.TabIndex = 0;
			this.buttonApply.Text = "Apply";
			this.buttonApply.UseVisualStyleBackColor = true;
			this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
			// 
			// secondBorderLimitTrackBar
			// 
			this.secondBorderLimitTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.secondBorderLimitTrackBar.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.secondBorderLimitTrackBar.Location = new System.Drawing.Point(4, 107);
			this.secondBorderLimitTrackBar.Margin = new System.Windows.Forms.Padding(2);
			this.secondBorderLimitTrackBar.Maximum = 180;
			this.secondBorderLimitTrackBar.Name = "secondBorderLimitTrackBar";
			this.secondBorderLimitTrackBar.Size = new System.Drawing.Size(721, 45);
			this.secondBorderLimitTrackBar.TabIndex = 17;
			this.secondBorderLimitTrackBar.ValueChanged += new System.EventHandler(this.secondBorderLimitTrackBar_ValueChanged);
			// 
			// secondBorderLimitLabel
			// 
			this.secondBorderLimitLabel.AutoSize = true;
			this.secondBorderLimitLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.secondBorderLimitLabel.Location = new System.Drawing.Point(13, 91);
			this.secondBorderLimitLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.secondBorderLimitLabel.Name = "secondBorderLimitLabel";
			this.secondBorderLimitLabel.Size = new System.Drawing.Size(122, 13);
			this.secondBorderLimitLabel.TabIndex = 16;
			this.secondBorderLimitLabel.Text = "second border limit: ";
			// 
			// firstBorderLimitTrackBar
			// 
			this.firstBorderLimitTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.firstBorderLimitTrackBar.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.firstBorderLimitTrackBar.Location = new System.Drawing.Point(4, 43);
			this.firstBorderLimitTrackBar.Margin = new System.Windows.Forms.Padding(2);
			this.firstBorderLimitTrackBar.Maximum = 180;
			this.firstBorderLimitTrackBar.Name = "firstBorderLimitTrackBar";
			this.firstBorderLimitTrackBar.Size = new System.Drawing.Size(721, 45);
			this.firstBorderLimitTrackBar.TabIndex = 15;
			this.firstBorderLimitTrackBar.ValueChanged += new System.EventHandler(this.firstBorderLimitTrackBar_ValueChanged);
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClose.Location = new System.Drawing.Point(871, 454);
			this.buttonClose.Margin = new System.Windows.Forms.Padding(2);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(94, 28);
			this.buttonClose.TabIndex = 0;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
			// 
			// firstBorderLimitLabel
			// 
			this.firstBorderLimitLabel.AutoSize = true;
			this.firstBorderLimitLabel.Location = new System.Drawing.Point(13, 27);
			this.firstBorderLimitLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.firstBorderLimitLabel.Name = "firstBorderLimitLabel";
			this.firstBorderLimitLabel.Size = new System.Drawing.Size(82, 13);
			this.firstBorderLimitLabel.TabIndex = 13;
			this.firstBorderLimitLabel.Text = "first border limit: ";
			// 
			// labelNodeCount
			// 
			this.labelNodeCount.AutoSize = true;
			this.labelNodeCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.labelNodeCount.Location = new System.Drawing.Point(4, 21);
			this.labelNodeCount.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelNodeCount.Name = "labelNodeCount";
			this.labelNodeCount.Size = new System.Drawing.Size(69, 13);
			this.labelNodeCount.TabIndex = 21;
			this.labelNodeCount.Text = "Node count: ";
			// 
			// labelElementCount
			// 
			this.labelElementCount.AutoSize = true;
			this.labelElementCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.labelElementCount.Location = new System.Drawing.Point(4, 35);
			this.labelElementCount.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelElementCount.Name = "labelElementCount";
			this.labelElementCount.Size = new System.Drawing.Size(81, 13);
			this.labelElementCount.TabIndex = 22;
			this.labelElementCount.Text = "Element count: ";
			// 
			// groupBoxBasicInfo
			// 
			this.groupBoxBasicInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxBasicInfo.Controls.Add(this.labelBeamCount);
			this.groupBoxBasicInfo.Controls.Add(this.labelEdgeCount);
			this.groupBoxBasicInfo.Controls.Add(this.labelFaceCount);
			this.groupBoxBasicInfo.Controls.Add(this.labelSurfaceItems);
			this.groupBoxBasicInfo.Controls.Add(this.labelNodeCount);
			this.groupBoxBasicInfo.Controls.Add(this.labelElementCount);
			this.groupBoxBasicInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.groupBoxBasicInfo.Location = new System.Drawing.Point(4, 4);
			this.groupBoxBasicInfo.Margin = new System.Windows.Forms.Padding(2);
			this.groupBoxBasicInfo.Name = "groupBoxBasicInfo";
			this.groupBoxBasicInfo.Padding = new System.Windows.Forms.Padding(2);
			this.groupBoxBasicInfo.Size = new System.Drawing.Size(908, 402);
			this.groupBoxBasicInfo.TabIndex = 23;
			this.groupBoxBasicInfo.TabStop = false;
			this.groupBoxBasicInfo.Text = "Mesh characteristics";
			// 
			// labelBeamCount
			// 
			this.labelBeamCount.AutoSize = true;
			this.labelBeamCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.labelBeamCount.Location = new System.Drawing.Point(4, 48);
			this.labelBeamCount.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelBeamCount.Name = "labelBeamCount";
			this.labelBeamCount.Size = new System.Drawing.Size(76, 13);
			this.labelBeamCount.TabIndex = 26;
			this.labelBeamCount.Text = "(Beam count: )";
			// 
			// labelEdgeCount
			// 
			this.labelEdgeCount.AutoSize = true;
			this.labelEdgeCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.labelEdgeCount.Location = new System.Drawing.Point(4, 103);
			this.labelEdgeCount.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelEdgeCount.Name = "labelEdgeCount";
			this.labelEdgeCount.Size = new System.Drawing.Size(65, 13);
			this.labelEdgeCount.TabIndex = 25;
			this.labelEdgeCount.Text = "Edge count:";
			// 
			// labelFaceCount
			// 
			this.labelFaceCount.AutoSize = true;
			this.labelFaceCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.labelFaceCount.Location = new System.Drawing.Point(4, 89);
			this.labelFaceCount.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelFaceCount.Name = "labelFaceCount";
			this.labelFaceCount.Size = new System.Drawing.Size(67, 13);
			this.labelFaceCount.TabIndex = 24;
			this.labelFaceCount.Text = "Face count: ";
			// 
			// labelSurfaceItems
			// 
			this.labelSurfaceItems.AutoSize = true;
			this.labelSurfaceItems.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.labelSurfaceItems.Location = new System.Drawing.Point(4, 73);
			this.labelSurfaceItems.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelSurfaceItems.Name = "labelSurfaceItems";
			this.labelSurfaceItems.Size = new System.Drawing.Size(71, 13);
			this.labelSurfaceItems.TabIndex = 23;
			this.labelSurfaceItems.Text = "Surface items";
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPageBasicInfo);
			this.tabControl.Controls.Add(this.tabPagePropertyDescriptions);
			this.tabControl.Controls.Add(this.tabPageEdgeAngleLimits);
			this.tabControl.Location = new System.Drawing.Point(9, 10);
			this.tabControl.Margin = new System.Windows.Forms.Padding(2);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(964, 440);
			this.tabControl.TabIndex = 26;
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
			// 
			// tabPageBasicInfo
			// 
			this.tabPageBasicInfo.Controls.Add(this.groupBoxBasicInfo);
			this.tabPageBasicInfo.Location = new System.Drawing.Point(4, 22);
			this.tabPageBasicInfo.Margin = new System.Windows.Forms.Padding(2);
			this.tabPageBasicInfo.Name = "tabPageBasicInfo";
			this.tabPageBasicInfo.Padding = new System.Windows.Forms.Padding(2);
			this.tabPageBasicInfo.Size = new System.Drawing.Size(956, 414);
			this.tabPageBasicInfo.TabIndex = 0;
			this.tabPageBasicInfo.Text = "Basic info";
			this.tabPageBasicInfo.UseVisualStyleBackColor = true;
			// 
			// tabPagePropertyDescriptions
			// 
			this.tabPagePropertyDescriptions.Controls.Add(this.dataGridViewPropertyDescriptions);
			this.tabPagePropertyDescriptions.Controls.Add(this.buttonEditComment);
			this.tabPagePropertyDescriptions.Controls.Add(this.buttonEditPropertyCommands);
			this.tabPagePropertyDescriptions.Location = new System.Drawing.Point(4, 22);
			this.tabPagePropertyDescriptions.Margin = new System.Windows.Forms.Padding(2);
			this.tabPagePropertyDescriptions.Name = "tabPagePropertyDescriptions";
			this.tabPagePropertyDescriptions.Padding = new System.Windows.Forms.Padding(2);
			this.tabPagePropertyDescriptions.Size = new System.Drawing.Size(956, 414);
			this.tabPagePropertyDescriptions.TabIndex = 1;
			this.tabPagePropertyDescriptions.Text = "Property descriptions";
			this.tabPagePropertyDescriptions.UseVisualStyleBackColor = true;
			// 
			// dataGridViewPropertyDescriptions
			// 
			this.dataGridViewPropertyDescriptions.AllowUserToAddRows = false;
			this.dataGridViewPropertyDescriptions.AllowUserToDeleteRows = false;
			this.dataGridViewPropertyDescriptions.AllowUserToOrderColumns = true;
			this.dataGridViewPropertyDescriptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataGridViewPropertyDescriptions.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
			this.dataGridViewPropertyDescriptions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PropertyColumn,
            this.TargetEntityColumn,
            this.CommandsColumn,
            this.CommentColumn});
			this.dataGridViewPropertyDescriptions.Location = new System.Drawing.Point(4, 5);
			this.dataGridViewPropertyDescriptions.MultiSelect = false;
			this.dataGridViewPropertyDescriptions.Name = "dataGridViewPropertyDescriptions";
			this.dataGridViewPropertyDescriptions.ReadOnly = true;
			this.dataGridViewPropertyDescriptions.RowHeadersWidth = 20;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.dataGridViewPropertyDescriptions.RowsDefaultCellStyle = dataGridViewCellStyle2;
			this.dataGridViewPropertyDescriptions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.dataGridViewPropertyDescriptions.Size = new System.Drawing.Size(947, 371);
			this.dataGridViewPropertyDescriptions.TabIndex = 27;
			this.dataGridViewPropertyDescriptions.DoubleClick += new System.EventHandler(this.dataGridViewPropertyDescriptions_DoubleClick);
			this.dataGridViewPropertyDescriptions.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridViewPropertyDescriptions_KeyDown);
			// 
			// buttonEditComment
			// 
			this.buttonEditComment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonEditComment.Location = new System.Drawing.Point(858, 382);
			this.buttonEditComment.Name = "buttonEditComment";
			this.buttonEditComment.Size = new System.Drawing.Size(94, 27);
			this.buttonEditComment.TabIndex = 26;
			this.buttonEditComment.Text = "Edit comment";
			this.buttonEditComment.UseVisualStyleBackColor = true;
			this.buttonEditComment.Click += new System.EventHandler(this.buttonEditComment_Click);
			// 
			// buttonEditPropertyCommands
			// 
			this.buttonEditPropertyCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonEditPropertyCommands.Location = new System.Drawing.Point(752, 382);
			this.buttonEditPropertyCommands.Margin = new System.Windows.Forms.Padding(2);
			this.buttonEditPropertyCommands.Name = "buttonEditPropertyCommands";
			this.buttonEditPropertyCommands.Size = new System.Drawing.Size(101, 27);
			this.buttonEditPropertyCommands.TabIndex = 25;
			this.buttonEditPropertyCommands.Text = "Edit commands";
			this.buttonEditPropertyCommands.UseVisualStyleBackColor = true;
			this.buttonEditPropertyCommands.Click += new System.EventHandler(this.buttonEditPropertyCommands_Click);
			// 
			// tabPageEdgeAngleLimits
			// 
			this.tabPageEdgeAngleLimits.Controls.Add(this.label1);
			this.tabPageEdgeAngleLimits.Controls.Add(this.firstBorderLimitLabel);
			this.tabPageEdgeAngleLimits.Controls.Add(this.buttonApply);
			this.tabPageEdgeAngleLimits.Controls.Add(this.firstBorderLimitTrackBar);
			this.tabPageEdgeAngleLimits.Controls.Add(this.secondBorderLimitLabel);
			this.tabPageEdgeAngleLimits.Controls.Add(this.secondBorderLimitTrackBar);
			this.tabPageEdgeAngleLimits.Controls.Add(this.histogramViewer);
			this.tabPageEdgeAngleLimits.Location = new System.Drawing.Point(4, 22);
			this.tabPageEdgeAngleLimits.Margin = new System.Windows.Forms.Padding(2);
			this.tabPageEdgeAngleLimits.Name = "tabPageEdgeAngleLimits";
			this.tabPageEdgeAngleLimits.Padding = new System.Windows.Forms.Padding(2);
			this.tabPageEdgeAngleLimits.Size = new System.Drawing.Size(956, 414);
			this.tabPageEdgeAngleLimits.TabIndex = 2;
			this.tabPageEdgeAngleLimits.Text = "Edge angle limits";
			this.tabPageEdgeAngleLimits.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 146);
			this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(117, 13);
			this.label1.TabIndex = 19;
			this.label1.Text = "Edge angles histogram:";
			// 
			// histogramViewer
			// 
			this.histogramViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.histogramViewer.BackColor = System.Drawing.SystemColors.Info;
			this.histogramViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.histogramViewer.FirstLimit = 0;
			this.histogramViewer.Location = new System.Drawing.Point(16, 161);
			this.histogramViewer.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.histogramViewer.Name = "histogramViewer";
			this.histogramViewer.SecondLimit = 0;
			this.histogramViewer.Size = new System.Drawing.Size(709, 209);
			this.histogramViewer.TabIndex = 18;
			// 
			// PropertyColumn
			// 
			this.PropertyColumn.HeaderText = "Property";
			this.PropertyColumn.Name = "PropertyColumn";
			this.PropertyColumn.ReadOnly = true;
			this.PropertyColumn.Width = 80;
			// 
			// TargetEntityColumn
			// 
			this.TargetEntityColumn.HeaderText = "Entity";
			this.TargetEntityColumn.Name = "TargetEntityColumn";
			this.TargetEntityColumn.ReadOnly = true;
			this.TargetEntityColumn.Width = 60;
			// 
			// CommandsColumn
			// 
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.CommandsColumn.DefaultCellStyle = dataGridViewCellStyle1;
			this.CommandsColumn.HeaderText = "Commands";
			this.CommandsColumn.Name = "CommandsColumn";
			this.CommandsColumn.ReadOnly = true;
			this.CommandsColumn.Width = 540;
			// 
			// CommentColumn
			// 
			this.CommentColumn.HeaderText = "Comment";
			this.CommentColumn.Name = "CommentColumn";
			this.CommentColumn.ReadOnly = true;
			this.CommentColumn.Width = 220;
			// 
			// MeshInfoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(982, 492);
			this.Controls.Add(this.tabControl);
			this.Controls.Add(this.buttonClose);
			this.Margin = new System.Windows.Forms.Padding(2);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MeshInfoForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Mesh info";
			((System.ComponentModel.ISupportInitialize)(this.secondBorderLimitTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.firstBorderLimitTrackBar)).EndInit();
			this.groupBoxBasicInfo.ResumeLayout(false);
			this.groupBoxBasicInfo.PerformLayout();
			this.tabControl.ResumeLayout(false);
			this.tabPageBasicInfo.ResumeLayout(false);
			this.tabPagePropertyDescriptions.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewPropertyDescriptions)).EndInit();
			this.tabPageEdgeAngleLimits.ResumeLayout(false);
			this.tabPageEdgeAngleLimits.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button buttonApply;
		private HistogramViewer histogramViewer;
		private System.Windows.Forms.TrackBar secondBorderLimitTrackBar;
		private System.Windows.Forms.Label secondBorderLimitLabel;
		private System.Windows.Forms.TrackBar firstBorderLimitTrackBar;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.Label firstBorderLimitLabel;
		private System.Windows.Forms.Label labelNodeCount;
		private System.Windows.Forms.Label labelElementCount;
		private System.Windows.Forms.GroupBox groupBoxBasicInfo;
		private System.Windows.Forms.Label labelFaceCount;
		private System.Windows.Forms.Label labelSurfaceItems;
		private System.Windows.Forms.Label labelEdgeCount;
		private System.Windows.Forms.Label labelBeamCount;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPageBasicInfo;
		private System.Windows.Forms.TabPage tabPagePropertyDescriptions;
		private System.Windows.Forms.TabPage tabPageEdgeAngleLimits;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonEditPropertyCommands;
		private System.Windows.Forms.Button buttonEditComment;
		private System.Windows.Forms.DataGridView dataGridViewPropertyDescriptions;
		private System.Windows.Forms.DataGridViewTextBoxColumn PropertyColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn TargetEntityColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn CommandsColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn CommentColumn;
	}
}