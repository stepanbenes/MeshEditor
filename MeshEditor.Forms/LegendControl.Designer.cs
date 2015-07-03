namespace MeshEditor.Forms
{
	partial class LegendControl
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
			this.labelLegend = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// labelLegend
			// 
			this.labelLegend.AutoSize = true;
			this.labelLegend.ForeColor = System.Drawing.Color.Red;
			this.labelLegend.Location = new System.Drawing.Point(26, 20);
			this.labelLegend.Name = "labelLegend";
			this.labelLegend.Size = new System.Drawing.Size(43, 13);
			this.labelLegend.TabIndex = 0;
			this.labelLegend.Text = "Legend";
			// 
			// LegendControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Transparent;
			this.Controls.Add(this.labelLegend);
			this.Name = "LegendControl";
			this.Size = new System.Drawing.Size(92, 51);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelLegend;

	}
}
