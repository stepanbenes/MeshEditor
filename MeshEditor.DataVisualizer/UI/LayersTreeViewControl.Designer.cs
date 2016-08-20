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
			this.treeViewLayers = new System.Windows.Forms.TreeView();
			this.labelCaption = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// treeViewLayers
			// 
			this.treeViewLayers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.treeViewLayers.CheckBoxes = true;
			this.treeViewLayers.Location = new System.Drawing.Point(3, 16);
			this.treeViewLayers.Name = "treeViewLayers";
			this.treeViewLayers.Size = new System.Drawing.Size(144, 131);
			this.treeViewLayers.TabIndex = 0;
			// 
			// labelCaption
			// 
			this.labelCaption.AutoSize = true;
			this.labelCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelCaption.Location = new System.Drawing.Point(0, 0);
			this.labelCaption.Name = "labelCaption";
			this.labelCaption.Size = new System.Drawing.Size(44, 13);
			this.labelCaption.TabIndex = 1;
			this.labelCaption.Text = "Layers";
			// 
			// LayersTreeViewControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.labelCaption);
			this.Controls.Add(this.treeViewLayers);
			this.Name = "LayersTreeViewControl";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TreeView treeViewLayers;
		private System.Windows.Forms.Label labelCaption;
	}
}
