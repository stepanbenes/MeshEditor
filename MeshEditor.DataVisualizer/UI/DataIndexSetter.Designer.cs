namespace MeshEditor.DataVisualizer.UI
{
	partial class DataIndexSetter
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
			this.comboBoxDataType = new System.Windows.Forms.ComboBox();
			this.comboBoxDataComponent = new System.Windows.Forms.ComboBox();
			this.trackBarCurrentTime = new System.Windows.Forms.TrackBar();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxCurrentTime = new System.Windows.Forms.TextBox();
			this.comboBoxDataTime = new System.Windows.Forms.ComboBox();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCurrentTime)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "Data type";
			// 
			// comboBoxDataType
			// 
			this.comboBoxDataType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxDataType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDataType.FormattingEnabled = true;
			this.comboBoxDataType.Location = new System.Drawing.Point(3, 16);
			this.comboBoxDataType.Name = "comboBoxDataType";
			this.comboBoxDataType.Size = new System.Drawing.Size(361, 21);
			this.comboBoxDataType.TabIndex = 4;
			this.comboBoxDataType.SelectedIndexChanged += new System.EventHandler(this.comboBoxDataType_SelectedIndexChanged);
			// 
			// comboBoxDataComponent
			// 
			this.comboBoxDataComponent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxDataComponent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDataComponent.FormattingEnabled = true;
			this.comboBoxDataComponent.Location = new System.Drawing.Point(3, 43);
			this.comboBoxDataComponent.Name = "comboBoxDataComponent";
			this.comboBoxDataComponent.Size = new System.Drawing.Size(361, 21);
			this.comboBoxDataComponent.TabIndex = 5;
			this.comboBoxDataComponent.SelectedIndexChanged += new System.EventHandler(this.comboBoxDataComponent_SelectedIndexChanged);
			// 
			// trackBarCurrentTime
			// 
			this.trackBarCurrentTime.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBarCurrentTime.BackColor = System.Drawing.Color.White;
			this.trackBarCurrentTime.Location = new System.Drawing.Point(0, 97);
			this.trackBarCurrentTime.Maximum = 50;
			this.trackBarCurrentTime.Name = "trackBarCurrentTime";
			this.trackBarCurrentTime.Size = new System.Drawing.Size(367, 45);
			this.trackBarCurrentTime.TabIndex = 9;
			this.trackBarCurrentTime.Scroll += new System.EventHandler(this.trackBarCurrentTime_Scroll);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 74);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(30, 13);
			this.label2.TabIndex = 10;
			this.label2.Text = "Time";
			// 
			// textBoxCurrentTime
			// 
			this.textBoxCurrentTime.Location = new System.Drawing.Point(40, 71);
			this.textBoxCurrentTime.Name = "textBoxCurrentTime";
			this.textBoxCurrentTime.Size = new System.Drawing.Size(77, 20);
			this.textBoxCurrentTime.TabIndex = 7;
			this.textBoxCurrentTime.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxCurrentTime_KeyPress);
			this.textBoxCurrentTime.Leave += new System.EventHandler(this.textBoxCurrentTime_Leave);
			// 
			// comboBoxDataTime
			// 
			this.comboBoxDataTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDataTime.FormattingEnabled = true;
			this.comboBoxDataTime.Location = new System.Drawing.Point(4, 90);
			this.comboBoxDataTime.Name = "comboBoxDataTime";
			this.comboBoxDataTime.Size = new System.Drawing.Size(87, 21);
			this.comboBoxDataTime.TabIndex = 8;
			this.comboBoxDataTime.SelectedIndexChanged += new System.EventHandler(this.comboBoxTime_SelectedIndexChanged);
			// 
			// DataIndexSetter
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.trackBarCurrentTime);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.textBoxCurrentTime);
			this.Controls.Add(this.comboBoxDataTime);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBoxDataType);
			this.Controls.Add(this.comboBoxDataComponent);
			this.Name = "DataIndexSetter";
			this.Size = new System.Drawing.Size(367, 145);
			((System.ComponentModel.ISupportInitialize)(this.trackBarCurrentTime)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxDataType;
		private System.Windows.Forms.ComboBox comboBoxDataComponent;
		private System.Windows.Forms.TrackBar trackBarCurrentTime;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxCurrentTime;
		private System.Windows.Forms.ComboBox comboBoxDataTime;
	}
}
