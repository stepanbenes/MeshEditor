namespace MeshEditor.DataVisualizer.UI
{
	partial class LayersForm
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
			this.checkedListBoxLayers = new System.Windows.Forms.CheckedListBox();
			this.buttonAddCrossSection = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.numericUpDownCount = new System.Windows.Forms.NumericUpDown();
			this.textBoxToOffset = new System.Windows.Forms.TextBox();
			this.textBoxFromOffset = new System.Windows.Forms.TextBox();
			this.comboBoxDirection = new System.Windows.Forms.ComboBox();
			this.buttonRemove = new System.Windows.Forms.Button();
			this.tabControlSectionSettings = new System.Windows.Forms.TabControl();
			this.tabPageCrossSection = new System.Windows.Forms.TabPage();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.tabPageIsoSurface = new System.Windows.Forms.TabPage();
			this.textBoxIsoSurfaceDataValue = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.buttonAddIsoSurface = new System.Windows.Forms.Button();
			this.tabPageLayerOptions = new System.Windows.Forms.TabPage();
			this.propertyGridLayerOptions = new System.Windows.Forms.PropertyGrid();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownCount)).BeginInit();
			this.tabControlSectionSettings.SuspendLayout();
			this.tabPageCrossSection.SuspendLayout();
			this.tabPageIsoSurface.SuspendLayout();
			this.tabPageLayerOptions.SuspendLayout();
			this.SuspendLayout();
			// 
			// checkedListBoxLayers
			// 
			this.checkedListBoxLayers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.checkedListBoxLayers.FormattingEnabled = true;
			this.checkedListBoxLayers.Location = new System.Drawing.Point(3, 3);
			this.checkedListBoxLayers.Name = "checkedListBoxLayers";
			this.checkedListBoxLayers.Size = new System.Drawing.Size(326, 229);
			this.checkedListBoxLayers.TabIndex = 0;
			this.checkedListBoxLayers.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxLayers_ItemCheck);
			this.checkedListBoxLayers.SelectedIndexChanged += new System.EventHandler(this.checkedListBoxLayers_SelectedIndexChanged);
			// 
			// buttonAddCrossSection
			// 
			this.buttonAddCrossSection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonAddCrossSection.Location = new System.Drawing.Point(6, 102);
			this.buttonAddCrossSection.Name = "buttonAddCrossSection";
			this.buttonAddCrossSection.Size = new System.Drawing.Size(75, 23);
			this.buttonAddCrossSection.TabIndex = 1;
			this.buttonAddCrossSection.Text = "Add";
			this.buttonAddCrossSection.UseVisualStyleBackColor = true;
			this.buttonAddCrossSection.Click += new System.EventHandler(this.buttonAddCrossSection_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(165, 56);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(35, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "Count";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(84, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(20, 13);
			this.label2.TabIndex = 7;
			this.label2.Text = "To";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 56);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(30, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "From";
			// 
			// numericUpDownCount
			// 
			this.numericUpDownCount.Location = new System.Drawing.Point(168, 73);
			this.numericUpDownCount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericUpDownCount.Name = "numericUpDownCount";
			this.numericUpDownCount.Size = new System.Drawing.Size(60, 20);
			this.numericUpDownCount.TabIndex = 5;
			this.numericUpDownCount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// textBoxToOffset
			// 
			this.textBoxToOffset.Location = new System.Drawing.Point(87, 72);
			this.textBoxToOffset.Name = "textBoxToOffset";
			this.textBoxToOffset.Size = new System.Drawing.Size(75, 20);
			this.textBoxToOffset.TabIndex = 4;
			this.textBoxToOffset.Text = "1.0";
			// 
			// textBoxFromOffset
			// 
			this.textBoxFromOffset.Location = new System.Drawing.Point(6, 72);
			this.textBoxFromOffset.Name = "textBoxFromOffset";
			this.textBoxFromOffset.Size = new System.Drawing.Size(75, 20);
			this.textBoxFromOffset.TabIndex = 3;
			this.textBoxFromOffset.Text = "0.0";
			// 
			// comboBoxDirection
			// 
			this.comboBoxDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDirection.FormattingEnabled = true;
			this.comboBoxDirection.Location = new System.Drawing.Point(61, 6);
			this.comboBoxDirection.Name = "comboBoxDirection";
			this.comboBoxDirection.Size = new System.Drawing.Size(75, 21);
			this.comboBoxDirection.TabIndex = 2;
			// 
			// buttonRemove
			// 
			this.buttonRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonRemove.Enabled = false;
			this.buttonRemove.Location = new System.Drawing.Point(3, 238);
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.Size = new System.Drawing.Size(75, 23);
			this.buttonRemove.TabIndex = 3;
			this.buttonRemove.Text = "Remove";
			this.buttonRemove.UseVisualStyleBackColor = true;
			this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
			// 
			// tabControlSectionSettings
			// 
			this.tabControlSectionSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControlSectionSettings.Controls.Add(this.tabPageCrossSection);
			this.tabControlSectionSettings.Controls.Add(this.tabPageIsoSurface);
			this.tabControlSectionSettings.Controls.Add(this.tabPageLayerOptions);
			this.tabControlSectionSettings.Location = new System.Drawing.Point(3, 267);
			this.tabControlSectionSettings.Name = "tabControlSectionSettings";
			this.tabControlSectionSettings.SelectedIndex = 0;
			this.tabControlSectionSettings.Size = new System.Drawing.Size(326, 157);
			this.tabControlSectionSettings.TabIndex = 5;
			// 
			// tabPageCrossSection
			// 
			this.tabPageCrossSection.Controls.Add(this.label6);
			this.tabPageCrossSection.Controls.Add(this.label5);
			this.tabPageCrossSection.Controls.Add(this.label3);
			this.tabPageCrossSection.Controls.Add(this.buttonAddCrossSection);
			this.tabPageCrossSection.Controls.Add(this.label2);
			this.tabPageCrossSection.Controls.Add(this.comboBoxDirection);
			this.tabPageCrossSection.Controls.Add(this.label1);
			this.tabPageCrossSection.Controls.Add(this.textBoxFromOffset);
			this.tabPageCrossSection.Controls.Add(this.numericUpDownCount);
			this.tabPageCrossSection.Controls.Add(this.textBoxToOffset);
			this.tabPageCrossSection.Location = new System.Drawing.Point(4, 22);
			this.tabPageCrossSection.Name = "tabPageCrossSection";
			this.tabPageCrossSection.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageCrossSection.Size = new System.Drawing.Size(318, 131);
			this.tabPageCrossSection.TabIndex = 0;
			this.tabPageCrossSection.Text = "Cross-section";
			this.tabPageCrossSection.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(6, 37);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(77, 13);
			this.label6.TabIndex = 10;
			this.label6.Text = "Relative Offset";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 9);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(49, 13);
			this.label5.TabIndex = 9;
			this.label5.Text = "Direction";
			// 
			// tabPageIsoSurface
			// 
			this.tabPageIsoSurface.Controls.Add(this.textBoxIsoSurfaceDataValue);
			this.tabPageIsoSurface.Controls.Add(this.label4);
			this.tabPageIsoSurface.Controls.Add(this.buttonAddIsoSurface);
			this.tabPageIsoSurface.Location = new System.Drawing.Point(4, 22);
			this.tabPageIsoSurface.Name = "tabPageIsoSurface";
			this.tabPageIsoSurface.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageIsoSurface.Size = new System.Drawing.Size(318, 131);
			this.tabPageIsoSurface.TabIndex = 1;
			this.tabPageIsoSurface.Text = "Iso-surface";
			this.tabPageIsoSurface.UseVisualStyleBackColor = true;
			// 
			// textBoxIsoSurfaceDataValue
			// 
			this.textBoxIsoSurfaceDataValue.Location = new System.Drawing.Point(6, 25);
			this.textBoxIsoSurfaceDataValue.Name = "textBoxIsoSurfaceDataValue";
			this.textBoxIsoSurfaceDataValue.Size = new System.Drawing.Size(75, 20);
			this.textBoxIsoSurfaceDataValue.TabIndex = 12;
			this.textBoxIsoSurfaceDataValue.Text = "0.0";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 9);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(59, 13);
			this.label4.TabIndex = 11;
			this.label4.Text = "Data value";
			// 
			// buttonAddIsoSurface
			// 
			this.buttonAddIsoSurface.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonAddIsoSurface.Location = new System.Drawing.Point(6, 102);
			this.buttonAddIsoSurface.Name = "buttonAddIsoSurface";
			this.buttonAddIsoSurface.Size = new System.Drawing.Size(75, 23);
			this.buttonAddIsoSurface.TabIndex = 10;
			this.buttonAddIsoSurface.Text = "Add";
			this.buttonAddIsoSurface.UseVisualStyleBackColor = true;
			this.buttonAddIsoSurface.Click += new System.EventHandler(this.buttonAddIsoSurface_Click);
			// 
			// tabPageLayerOptions
			// 
			this.tabPageLayerOptions.Controls.Add(this.propertyGridLayerOptions);
			this.tabPageLayerOptions.Location = new System.Drawing.Point(4, 22);
			this.tabPageLayerOptions.Name = "tabPageLayerOptions";
			this.tabPageLayerOptions.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageLayerOptions.Size = new System.Drawing.Size(318, 131);
			this.tabPageLayerOptions.TabIndex = 2;
			this.tabPageLayerOptions.Text = "Layer options";
			this.tabPageLayerOptions.UseVisualStyleBackColor = true;
			// 
			// propertyGridLayerOptions
			// 
			this.propertyGridLayerOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGridLayerOptions.HelpVisible = false;
			this.propertyGridLayerOptions.Location = new System.Drawing.Point(0, 3);
			this.propertyGridLayerOptions.Name = "propertyGridLayerOptions";
			this.propertyGridLayerOptions.Size = new System.Drawing.Size(315, 128);
			this.propertyGridLayerOptions.TabIndex = 0;
			this.propertyGridLayerOptions.ToolbarVisible = false;
			this.propertyGridLayerOptions.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGridLayerOptions_PropertyValueChanged);
			// 
			// LayersForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(332, 426);
			this.Controls.Add(this.tabControlSectionSettings);
			this.Controls.Add(this.buttonRemove);
			this.Controls.Add(this.checkedListBoxLayers);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "LayersForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Layers";
			this.TopMost = true;
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownCount)).EndInit();
			this.tabControlSectionSettings.ResumeLayout(false);
			this.tabPageCrossSection.ResumeLayout(false);
			this.tabPageCrossSection.PerformLayout();
			this.tabPageIsoSurface.ResumeLayout(false);
			this.tabPageIsoSurface.PerformLayout();
			this.tabPageLayerOptions.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.CheckedListBox checkedListBoxLayers;
		private System.Windows.Forms.Button buttonAddCrossSection;
		private System.Windows.Forms.ComboBox comboBoxDirection;
		private System.Windows.Forms.Button buttonRemove;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown numericUpDownCount;
		private System.Windows.Forms.TextBox textBoxToOffset;
		private System.Windows.Forms.TextBox textBoxFromOffset;
		private System.Windows.Forms.TabControl tabControlSectionSettings;
		private System.Windows.Forms.TabPage tabPageCrossSection;
		private System.Windows.Forms.TabPage tabPageIsoSurface;
		private System.Windows.Forms.Button buttonAddIsoSurface;
		private System.Windows.Forms.TextBox textBoxIsoSurfaceDataValue;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TabPage tabPageLayerOptions;
		private System.Windows.Forms.PropertyGrid propertyGridLayerOptions;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
	}
}