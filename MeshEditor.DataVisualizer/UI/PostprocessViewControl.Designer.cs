namespace MeshEditor.WinUI
{
	partial class PostprocessViewControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.layersTreeView = new MeshEditor.WinUI.LayersTreeViewControl();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.leftSplitContainer = new System.Windows.Forms.SplitContainer();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.leftSplitContainer)).BeginInit();
			this.leftSplitContainer.Panel1.SuspendLayout();
			this.leftSplitContainer.Panel2.SuspendLayout();
			this.leftSplitContainer.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.leftSplitContainer);
			this.splitContainer1.Size = new System.Drawing.Size(1090, 861);
			this.splitContainer1.SplitterDistance = 363;
			this.splitContainer1.TabIndex = 0;
			// 
			// layersTreeView
			// 
			this.layersTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.layersTreeView.Location = new System.Drawing.Point(0, 0);
			this.layersTreeView.Name = "layersTreeView";
			this.layersTreeView.Size = new System.Drawing.Size(363, 430);
			this.layersTreeView.TabIndex = 1;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(363, 427);
			this.tableLayoutPanel1.TabIndex = 2;
			// 
			// leftSplitContainer
			// 
			this.leftSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.leftSplitContainer.Location = new System.Drawing.Point(0, 0);
			this.leftSplitContainer.Name = "leftSplitContainer";
			this.leftSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// leftSplitContainer.Panel1
			// 
			this.leftSplitContainer.Panel1.Controls.Add(this.layersTreeView);
			// 
			// leftSplitContainer.Panel2
			// 
			this.leftSplitContainer.Panel2.Controls.Add(this.tableLayoutPanel1);
			this.leftSplitContainer.Size = new System.Drawing.Size(363, 861);
			this.leftSplitContainer.SplitterDistance = 430;
			this.leftSplitContainer.TabIndex = 3;
			// 
			// PostprocessViewControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Name = "PostprocessViewControl";
			this.Size = new System.Drawing.Size(1090, 861);
			this.splitContainer1.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.leftSplitContainer.Panel1.ResumeLayout(false);
			this.leftSplitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.leftSplitContainer)).EndInit();
			this.leftSplitContainer.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private MeshEditor.WinUI.LayersTreeViewControl layersTreeView;
		private System.Windows.Forms.SplitContainer leftSplitContainer;
	}
}
