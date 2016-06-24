namespace MeshEditor.WinUI
{
	partial class DataSelectionControl
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
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxField = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBoxComponent = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxTimeStep = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Field";
			// 
			// comboBoxField
			// 
			this.comboBoxField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxField.FormattingEnabled = true;
			this.comboBoxField.Location = new System.Drawing.Point(3, 16);
			this.comboBoxField.Name = "comboBoxField";
			this.comboBoxField.Size = new System.Drawing.Size(214, 21);
			this.comboBoxField.TabIndex = 1;
			this.comboBoxField.SelectedIndexChanged += new System.EventHandler(this.comboBoxField_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 40);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(61, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Component";
			// 
			// comboBoxComponent
			// 
			this.comboBoxComponent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxComponent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxComponent.FormattingEnabled = true;
			this.comboBoxComponent.Location = new System.Drawing.Point(3, 56);
			this.comboBoxComponent.Name = "comboBoxComponent";
			this.comboBoxComponent.Size = new System.Drawing.Size(214, 21);
			this.comboBoxComponent.TabIndex = 3;
			this.comboBoxComponent.SelectedIndexChanged += new System.EventHandler(this.comboBoxComponent_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 80);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(53, 13);
			this.label3.TabIndex = 4;
			this.label3.Text = "Time step";
			// 
			// comboBoxTimeStep
			// 
			this.comboBoxTimeStep.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxTimeStep.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxTimeStep.FormattingEnabled = true;
			this.comboBoxTimeStep.Location = new System.Drawing.Point(3, 96);
			this.comboBoxTimeStep.Name = "comboBoxTimeStep";
			this.comboBoxTimeStep.Size = new System.Drawing.Size(214, 21);
			this.comboBoxTimeStep.TabIndex = 5;
			this.comboBoxTimeStep.SelectedIndexChanged += new System.EventHandler(this.comboBoxTimeStep_SelectedIndexChanged);
			// 
			// DataSelectionControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.comboBoxTimeStep);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.comboBoxComponent);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.comboBoxField);
			this.Controls.Add(this.label1);
			this.Name = "DataSelectionControl";
			this.Size = new System.Drawing.Size(220, 298);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxField;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBoxComponent;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboBoxTimeStep;
	}
}
