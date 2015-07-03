namespace MeshEditor.DataVisualizer.UI
{
	partial class DataPickerForm
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
			this.dataPickerControl = new MeshEditor.DataVisualizer.UI.DataPickerControl();
			this.SuspendLayout();
			// 
			// dataPickerControl
			// 
			this.dataPickerControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataPickerControl.DataVisualizer = null;
			this.dataPickerControl.Location = new System.Drawing.Point(3, 2);
			this.dataPickerControl.LongOpNotifier = null;
			this.dataPickerControl.Margin = new System.Windows.Forms.Padding(5);
			this.dataPickerControl.Name = "dataPickerControl";
			this.dataPickerControl.Size = new System.Drawing.Size(395, 521);
			this.dataPickerControl.TabIndex = 0;
			// 
			// DataPickerForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(397, 524);
			this.Controls.Add(this.dataPickerControl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.Name = "DataPickerForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Data picker";
			this.TopMost = true;
			this.ResumeLayout(false);

		}

		#endregion

		private DataPickerControl dataPickerControl;
	}
}