namespace MeshEditor.DataVisualizer.UI
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
			this.comboBoxComponent = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxTimeStep = new System.Windows.Forms.ComboBox();
			this.labelCaption = new System.Windows.Forms.Label();
			this.comboBoxVectorField = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(4, 78);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(55, 17);
			this.label1.TabIndex = 0;
			this.label1.Text = "Scalars";
			// 
			// comboBoxField
			// 
			this.comboBoxField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxField.FormattingEnabled = true;
			this.comboBoxField.Location = new System.Drawing.Point(4, 98);
			this.comboBoxField.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxField.Name = "comboBoxField";
			this.comboBoxField.Size = new System.Drawing.Size(284, 24);
			this.comboBoxField.TabIndex = 1;
			this.comboBoxField.SelectedIndexChanged += new System.EventHandler(this.comboBoxField_SelectedIndexChanged);
			// 
			// comboBoxComponent
			// 
			this.comboBoxComponent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxComponent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxComponent.FormattingEnabled = true;
			this.comboBoxComponent.Location = new System.Drawing.Point(4, 130);
			this.comboBoxComponent.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxComponent.Name = "comboBoxComponent";
			this.comboBoxComponent.Size = new System.Drawing.Size(284, 24);
			this.comboBoxComponent.TabIndex = 3;
			this.comboBoxComponent.SelectedIndexChanged += new System.EventHandler(this.comboBoxComponent_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(4, 28);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(70, 17);
			this.label3.TabIndex = 4;
			this.label3.Text = "Time step";
			// 
			// comboBoxTimeStep
			// 
			this.comboBoxTimeStep.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxTimeStep.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxTimeStep.FormattingEnabled = true;
			this.comboBoxTimeStep.Location = new System.Drawing.Point(4, 47);
			this.comboBoxTimeStep.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxTimeStep.Name = "comboBoxTimeStep";
			this.comboBoxTimeStep.Size = new System.Drawing.Size(284, 24);
			this.comboBoxTimeStep.TabIndex = 5;
			this.comboBoxTimeStep.SelectedIndexChanged += new System.EventHandler(this.comboBoxTimeStep_SelectedIndexChanged);
			// 
			// labelCaption
			// 
			this.labelCaption.AutoSize = true;
			this.labelCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelCaption.Location = new System.Drawing.Point(0, 0);
			this.labelCaption.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelCaption.Name = "labelCaption";
			this.labelCaption.Size = new System.Drawing.Size(112, 17);
			this.labelCaption.TabIndex = 6;
			this.labelCaption.Text = "Data selection";
			// 
			// comboBoxVectorField
			// 
			this.comboBoxVectorField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxVectorField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxVectorField.FormattingEnabled = true;
			this.comboBoxVectorField.Location = new System.Drawing.Point(5, 183);
			this.comboBoxVectorField.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxVectorField.Name = "comboBoxVectorField";
			this.comboBoxVectorField.Size = new System.Drawing.Size(284, 24);
			this.comboBoxVectorField.TabIndex = 8;
			this.comboBoxVectorField.SelectedIndexChanged += new System.EventHandler(this.comboBoxVectorField_SelectedIndexChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(4, 162);
			this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(56, 17);
			this.label4.TabIndex = 7;
			this.label4.Text = "Vectors";
			// 
			// DataSelectionControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.comboBoxVectorField);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.labelCaption);
			this.Controls.Add(this.comboBoxTimeStep);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.comboBoxComponent);
			this.Controls.Add(this.comboBoxField);
			this.Controls.Add(this.label1);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "DataSelectionControl";
			this.Size = new System.Drawing.Size(293, 367);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxField;
		private System.Windows.Forms.ComboBox comboBoxComponent;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboBoxTimeStep;
		private System.Windows.Forms.Label labelCaption;
		private System.Windows.Forms.ComboBox comboBoxVectorField;
		private System.Windows.Forms.Label label4;
	}
}
