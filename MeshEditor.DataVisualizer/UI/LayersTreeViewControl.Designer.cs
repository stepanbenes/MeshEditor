namespace MeshEditor.DataVisualizer.UI
{
	partial class LayersTreeViewControl
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
			this.components = new System.ComponentModel.Container();
			this.treeViewLayers = new System.Windows.Forms.TreeView();
			this.labelCaption = new System.Windows.Forms.Label();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.applyFilterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deformationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sliceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.isosurfaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteLayerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.reloadLayerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// treeViewLayers
			// 
			this.treeViewLayers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.treeViewLayers.CheckBoxes = true;
			this.treeViewLayers.HideSelection = false;
			this.treeViewLayers.Location = new System.Drawing.Point(4, 20);
			this.treeViewLayers.Margin = new System.Windows.Forms.Padding(4);
			this.treeViewLayers.Name = "treeViewLayers";
			this.treeViewLayers.Size = new System.Drawing.Size(191, 160);
			this.treeViewLayers.TabIndex = 0;
			this.treeViewLayers.MouseUp += new System.Windows.Forms.MouseEventHandler(this.treeViewLayers_MouseUp);
			// 
			// labelCaption
			// 
			this.labelCaption.AutoSize = true;
			this.labelCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelCaption.Location = new System.Drawing.Point(0, 0);
			this.labelCaption.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelCaption.Name = "labelCaption";
			this.labelCaption.Size = new System.Drawing.Size(57, 17);
			this.labelCaption.TabIndex = 1;
			this.labelCaption.Text = "Layers";
			// 
			// contextMenuStrip
			// 
			this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.applyFilterToolStripMenuItem,
            this.deleteLayerToolStripMenuItem,
            this.toolStripSeparator1,
            this.reloadLayerToolStripMenuItem});
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size(162, 82);
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
			// 
			// applyFilterToolStripMenuItem
			// 
			this.applyFilterToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deformationToolStripMenuItem,
            this.sliceToolStripMenuItem,
            this.isosurfaceToolStripMenuItem});
			this.applyFilterToolStripMenuItem.Name = "applyFilterToolStripMenuItem";
			this.applyFilterToolStripMenuItem.Size = new System.Drawing.Size(161, 24);
			this.applyFilterToolStripMenuItem.Text = "Apply filter...";
			// 
			// deformationToolStripMenuItem
			// 
			this.deformationToolStripMenuItem.Name = "deformationToolStripMenuItem";
			this.deformationToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
			this.deformationToolStripMenuItem.Text = "Deformation...";
			this.deformationToolStripMenuItem.Click += new System.EventHandler(this.deformationToolStripMenuItem_Click);
			// 
			// sliceToolStripMenuItem
			// 
			this.sliceToolStripMenuItem.Name = "sliceToolStripMenuItem";
			this.sliceToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
			this.sliceToolStripMenuItem.Text = "Slice...";
			this.sliceToolStripMenuItem.Click += new System.EventHandler(this.sliceToolStripMenuItem_Click);
			// 
			// isosurfaceToolStripMenuItem
			// 
			this.isosurfaceToolStripMenuItem.Enabled = false;
			this.isosurfaceToolStripMenuItem.Name = "isosurfaceToolStripMenuItem";
			this.isosurfaceToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
			this.isosurfaceToolStripMenuItem.Text = "Iso-surface...";
			// 
			// deleteLayerToolStripMenuItem
			// 
			this.deleteLayerToolStripMenuItem.Name = "deleteLayerToolStripMenuItem";
			this.deleteLayerToolStripMenuItem.Size = new System.Drawing.Size(161, 24);
			this.deleteLayerToolStripMenuItem.Text = "Delete layer";
			this.deleteLayerToolStripMenuItem.Click += new System.EventHandler(this.deleteLayerToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(158, 6);
			// 
			// reloadLayerToolStripMenuItem
			// 
			this.reloadLayerToolStripMenuItem.Name = "reloadLayerToolStripMenuItem";
			this.reloadLayerToolStripMenuItem.Size = new System.Drawing.Size(161, 24);
			this.reloadLayerToolStripMenuItem.Text = "Reload layer";
			this.reloadLayerToolStripMenuItem.Click += new System.EventHandler(this.reloadLayerToolStripMenuItem_Click);
			// 
			// LayersTreeViewControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.labelCaption);
			this.Controls.Add(this.treeViewLayers);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "LayersTreeViewControl";
			this.Size = new System.Drawing.Size(200, 185);
			this.contextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TreeView treeViewLayers;
		private System.Windows.Forms.Label labelCaption;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem applyFilterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteLayerToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem reloadLayerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deformationToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sliceToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem isosurfaceToolStripMenuItem;
	}
}
