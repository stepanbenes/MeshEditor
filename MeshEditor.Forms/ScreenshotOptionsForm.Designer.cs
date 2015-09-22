namespace MeshEditor.WinUI
{
	partial class ScreenshotOptionsForm
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
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.radioButtonWholeScene = new System.Windows.Forms.RadioButton();
			this.radioButtonSelectionArea = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(171, 95);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(2);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(63, 28);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(104, 95);
			this.buttonOK.Margin = new System.Windows.Forms.Padding(2);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(63, 28);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(117, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "Take a screenshot of...";
			// 
			// radioButtonWholeScene
			// 
			this.radioButtonWholeScene.AutoSize = true;
			this.radioButtonWholeScene.Location = new System.Drawing.Point(12, 40);
			this.radioButtonWholeScene.Name = "radioButtonWholeScene";
			this.radioButtonWholeScene.Size = new System.Drawing.Size(88, 17);
			this.radioButtonWholeScene.TabIndex = 5;
			this.radioButtonWholeScene.Text = "Whole scene";
			this.radioButtonWholeScene.UseVisualStyleBackColor = true;
			this.radioButtonWholeScene.CheckedChanged += new System.EventHandler(this.radioButtonWholeScene_CheckedChanged);
			// 
			// radioButtonSelectionArea
			// 
			this.radioButtonSelectionArea.AutoSize = true;
			this.radioButtonSelectionArea.Location = new System.Drawing.Point(13, 63);
			this.radioButtonSelectionArea.Name = "radioButtonSelectionArea";
			this.radioButtonSelectionArea.Size = new System.Drawing.Size(93, 17);
			this.radioButtonSelectionArea.TabIndex = 6;
			this.radioButtonSelectionArea.Text = "Selection area";
			this.radioButtonSelectionArea.UseVisualStyleBackColor = true;
			// 
			// ScreenshotOptionsForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(245, 134);
			this.Controls.Add(this.radioButtonSelectionArea);
			this.Controls.Add(this.radioButtonWholeScene);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ScreenshotOptionsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Screenshot options";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton radioButtonWholeScene;
		private System.Windows.Forms.RadioButton radioButtonSelectionArea;
	}
}