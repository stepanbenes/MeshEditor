namespace MeshEditor.DataVisualizer.UI
{
	partial class PostprocessViewControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
			this.leftSplitContainer = new System.Windows.Forms.SplitContainer();
			this.layersTreeView = new MeshEditor.DataVisualizer.UI.LayersTreeViewControl();
			this.dataSelectionControl = new MeshEditor.DataVisualizer.UI.DataSelectionControl();
			((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
			this.mainSplitContainer.Panel1.SuspendLayout();
			this.mainSplitContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.leftSplitContainer)).BeginInit();
			this.leftSplitContainer.Panel1.SuspendLayout();
			this.leftSplitContainer.Panel2.SuspendLayout();
			this.leftSplitContainer.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainSplitContainer
			// 
			this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainSplitContainer.Location = new System.Drawing.Point(0, 0);
			this.mainSplitContainer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.mainSplitContainer.Name = "mainSplitContainer";
			// 
			// mainSplitContainer.Panel1
			// 
			this.mainSplitContainer.Panel1.Controls.Add(this.leftSplitContainer);
			this.mainSplitContainer.Size = new System.Drawing.Size(1453, 1060);
			this.mainSplitContainer.SplitterDistance = 220;
			this.mainSplitContainer.SplitterWidth = 5;
			this.mainSplitContainer.TabIndex = 0;
			// 
			// leftSplitContainer
			// 
			this.leftSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.leftSplitContainer.Location = new System.Drawing.Point(0, 0);
			this.leftSplitContainer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.leftSplitContainer.Name = "leftSplitContainer";
			this.leftSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// leftSplitContainer.Panel1
			// 
			this.leftSplitContainer.Panel1.Controls.Add(this.layersTreeView);
			// 
			// leftSplitContainer.Panel2
			// 
			this.leftSplitContainer.Panel2.Controls.Add(this.dataSelectionControl);
			this.leftSplitContainer.Size = new System.Drawing.Size(483, 1060);
			this.leftSplitContainer.SplitterDistance = 243;
			this.leftSplitContainer.SplitterWidth = 5;
			this.leftSplitContainer.TabIndex = 3;
			// 
			// layersTreeView
			// 
			this.layersTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.layersTreeView.Location = new System.Drawing.Point(0, 0);
			this.layersTreeView.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
			this.layersTreeView.Name = "layersTreeView";
			this.layersTreeView.Size = new System.Drawing.Size(483, 243);
			this.layersTreeView.TabIndex = 1;
			// 
			// dataSelectionControl
			// 
			this.dataSelectionControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataSelectionControl.Location = new System.Drawing.Point(0, 0);
			this.dataSelectionControl.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
			this.dataSelectionControl.Name = "dataSelectionControl";
			this.dataSelectionControl.Size = new System.Drawing.Size(483, 812);
			this.dataSelectionControl.TabIndex = 0;
			// 
			// PostprocessViewControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.mainSplitContainer);
			this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.Name = "PostprocessViewControl";
			this.Size = new System.Drawing.Size(1453, 1060);
			this.mainSplitContainer.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
			this.mainSplitContainer.ResumeLayout(false);
			this.leftSplitContainer.Panel1.ResumeLayout(false);
			this.leftSplitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.leftSplitContainer)).EndInit();
			this.leftSplitContainer.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer mainSplitContainer;
		private MeshEditor.DataVisualizer.UI.LayersTreeViewControl layersTreeView;
		private System.Windows.Forms.SplitContainer leftSplitContainer;
		private MeshEditor.DataVisualizer.UI.DataSelectionControl dataSelectionControl;
	}
}
