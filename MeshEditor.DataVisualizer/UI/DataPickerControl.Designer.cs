namespace MeshEditor.DataVisualizer.UI
{
	partial class DataPickerControl
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
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxColorScaleType = new System.Windows.Forms.ComboBox();
			this.checkBoxDrawIsoAreas = new System.Windows.Forms.CheckBox();
			this.comboBoxIsoAreasSubIntervalNumber = new System.Windows.Forms.ComboBox();
			this.tabControlDisplayStyles = new System.Windows.Forms.TabControl();
			this.tabPageScalars = new System.Windows.Forms.TabPage();
			this.checkBoxVectorMagnitudes = new System.Windows.Forms.CheckBox();
			this.labelDisplayMethod = new System.Windows.Forms.Label();
			this.comboBoxDisplayMethod = new System.Windows.Forms.ComboBox();
			this.checkBoxShowLegend = new System.Windows.Forms.CheckBox();
			this.labelSubintervals = new System.Windows.Forms.Label();
			this.checkBoxShowScalarData = new System.Windows.Forms.CheckBox();
			this.colorScaleSetter = new MeshEditor.DataVisualizer.UI.ColorScaleSetter();
			this.dataIndexSetterScalars = new MeshEditor.DataVisualizer.UI.DataIndexSetter();
			this.tabPageVectors = new System.Windows.Forms.TabPage();
			this.checkBoxMoveEndOfArrowsToNodes = new System.Windows.Forms.CheckBox();
			this.textBoxVectorLengthFactor = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.checkBoxShowVectorData = new System.Windows.Forms.CheckBox();
			this.dataIndexSetterVectors = new MeshEditor.DataVisualizer.UI.DataIndexSetter();
			this.tabPageDeformations = new System.Windows.Forms.TabPage();
			this.groupBoxScale = new System.Windows.Forms.GroupBox();
			this.labelRelativeScale = new System.Windows.Forms.Label();
			this.textBoxScaleValue = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.radioButtonRelative = new System.Windows.Forms.RadioButton();
			this.radioButtonAbsolute = new System.Windows.Forms.RadioButton();
			this.trackBarDeformationMultiplier = new System.Windows.Forms.TrackBar();
			this.checkBoxDrawDeformed = new System.Windows.Forms.CheckBox();
			this.dataIndexSetterDeformations = new MeshEditor.DataVisualizer.UI.DataIndexSetter();
			this.tabControlDisplayStyles.SuspendLayout();
			this.tabPageScalars.SuspendLayout();
			this.tabPageVectors.SuspendLayout();
			this.tabPageDeformations.SuspendLayout();
			this.groupBoxScale.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarDeformationMultiplier)).BeginInit();
			this.SuspendLayout();
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 206);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(82, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Color scale type";
			// 
			// comboBoxColorScaleType
			// 
			this.comboBoxColorScaleType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxColorScaleType.FormattingEnabled = true;
			this.comboBoxColorScaleType.Location = new System.Drawing.Point(6, 222);
			this.comboBoxColorScaleType.Name = "comboBoxColorScaleType";
			this.comboBoxColorScaleType.Size = new System.Drawing.Size(119, 21);
			this.comboBoxColorScaleType.TabIndex = 10;
			this.comboBoxColorScaleType.SelectedIndexChanged += new System.EventHandler(this.comboBoxColorScaleType_SelectedIndexChanged);
			// 
			// checkBoxDrawIsoAreas
			// 
			this.checkBoxDrawIsoAreas.AutoSize = true;
			this.checkBoxDrawIsoAreas.Location = new System.Drawing.Point(6, 186);
			this.checkBoxDrawIsoAreas.Name = "checkBoxDrawIsoAreas";
			this.checkBoxDrawIsoAreas.Size = new System.Drawing.Size(119, 17);
			this.checkBoxDrawIsoAreas.TabIndex = 6;
			this.checkBoxDrawIsoAreas.Text = "Draw contour areas";
			this.checkBoxDrawIsoAreas.UseVisualStyleBackColor = true;
			this.checkBoxDrawIsoAreas.CheckedChanged += new System.EventHandler(this.checkBoxDrawIsoAreas_CheckedChanged);
			// 
			// comboBoxIsoAreasSubIntervalNumber
			// 
			this.comboBoxIsoAreasSubIntervalNumber.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxIsoAreasSubIntervalNumber.FormattingEnabled = true;
			this.comboBoxIsoAreasSubIntervalNumber.Location = new System.Drawing.Point(131, 184);
			this.comboBoxIsoAreasSubIntervalNumber.Name = "comboBoxIsoAreasSubIntervalNumber";
			this.comboBoxIsoAreasSubIntervalNumber.Size = new System.Drawing.Size(50, 21);
			this.comboBoxIsoAreasSubIntervalNumber.TabIndex = 7;
			this.comboBoxIsoAreasSubIntervalNumber.SelectedIndexChanged += new System.EventHandler(this.comboBoxIsoAreasSubIntervalNumber_SelectedIndexChanged);
			// 
			// tabControlDisplayStyles
			// 
			this.tabControlDisplayStyles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControlDisplayStyles.Controls.Add(this.tabPageScalars);
			this.tabControlDisplayStyles.Controls.Add(this.tabPageVectors);
			this.tabControlDisplayStyles.Controls.Add(this.tabPageDeformations);
			this.tabControlDisplayStyles.Location = new System.Drawing.Point(0, 0);
			this.tabControlDisplayStyles.Name = "tabControlDisplayStyles";
			this.tabControlDisplayStyles.SelectedIndex = 0;
			this.tabControlDisplayStyles.Size = new System.Drawing.Size(293, 415);
			this.tabControlDisplayStyles.TabIndex = 11;
			// 
			// tabPageScalars
			// 
			this.tabPageScalars.Controls.Add(this.checkBoxVectorMagnitudes);
			this.tabPageScalars.Controls.Add(this.labelDisplayMethod);
			this.tabPageScalars.Controls.Add(this.comboBoxDisplayMethod);
			this.tabPageScalars.Controls.Add(this.checkBoxShowLegend);
			this.tabPageScalars.Controls.Add(this.labelSubintervals);
			this.tabPageScalars.Controls.Add(this.checkBoxShowScalarData);
			this.tabPageScalars.Controls.Add(this.colorScaleSetter);
			this.tabPageScalars.Controls.Add(this.comboBoxIsoAreasSubIntervalNumber);
			this.tabPageScalars.Controls.Add(this.label3);
			this.tabPageScalars.Controls.Add(this.checkBoxDrawIsoAreas);
			this.tabPageScalars.Controls.Add(this.comboBoxColorScaleType);
			this.tabPageScalars.Controls.Add(this.dataIndexSetterScalars);
			this.tabPageScalars.Location = new System.Drawing.Point(4, 22);
			this.tabPageScalars.Name = "tabPageScalars";
			this.tabPageScalars.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageScalars.Size = new System.Drawing.Size(285, 389);
			this.tabPageScalars.TabIndex = 0;
			this.tabPageScalars.Text = "Scalars";
			this.tabPageScalars.UseVisualStyleBackColor = true;
			// 
			// checkBoxVectorMagnitudes
			// 
			this.checkBoxVectorMagnitudes.AutoSize = true;
			this.checkBoxVectorMagnitudes.Location = new System.Drawing.Point(136, 6);
			this.checkBoxVectorMagnitudes.Name = "checkBoxVectorMagnitudes";
			this.checkBoxVectorMagnitudes.Size = new System.Drawing.Size(143, 17);
			this.checkBoxVectorMagnitudes.TabIndex = 18;
			this.checkBoxVectorMagnitudes.Text = "Show vector magnitudes";
			this.checkBoxVectorMagnitudes.UseVisualStyleBackColor = true;
			this.checkBoxVectorMagnitudes.CheckedChanged += new System.EventHandler(this.checkBoxVectorMagnitudes_CheckedChanged);
			// 
			// labelDisplayMethod
			// 
			this.labelDisplayMethod.AutoSize = true;
			this.labelDisplayMethod.Location = new System.Drawing.Point(3, 161);
			this.labelDisplayMethod.Name = "labelDisplayMethod";
			this.labelDisplayMethod.Size = new System.Drawing.Size(70, 13);
			this.labelDisplayMethod.TabIndex = 17;
			this.labelDisplayMethod.Text = "Display mode";
			// 
			// comboBoxDisplayMethod
			// 
			this.comboBoxDisplayMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDisplayMethod.FormattingEnabled = true;
			this.comboBoxDisplayMethod.Location = new System.Drawing.Point(79, 158);
			this.comboBoxDisplayMethod.Name = "comboBoxDisplayMethod";
			this.comboBoxDisplayMethod.Size = new System.Drawing.Size(124, 21);
			this.comboBoxDisplayMethod.TabIndex = 16;
			this.comboBoxDisplayMethod.SelectedIndexChanged += new System.EventHandler(this.comboBoxDisplayMethod_SelectedIndexChanged);
			// 
			// checkBoxShowLegend
			// 
			this.checkBoxShowLegend.AutoSize = true;
			this.checkBoxShowLegend.Location = new System.Drawing.Point(131, 226);
			this.checkBoxShowLegend.Name = "checkBoxShowLegend";
			this.checkBoxShowLegend.Size = new System.Drawing.Size(88, 17);
			this.checkBoxShowLegend.TabIndex = 15;
			this.checkBoxShowLegend.Text = "Show legend";
			this.checkBoxShowLegend.UseVisualStyleBackColor = true;
			this.checkBoxShowLegend.CheckedChanged += new System.EventHandler(this.checkBoxShowLegend_CheckedChanged);
			// 
			// labelSubintervals
			// 
			this.labelSubintervals.AutoSize = true;
			this.labelSubintervals.Location = new System.Drawing.Point(187, 187);
			this.labelSubintervals.Name = "labelSubintervals";
			this.labelSubintervals.Size = new System.Drawing.Size(63, 13);
			this.labelSubintervals.TabIndex = 14;
			this.labelSubintervals.Text = "subintervals";
			// 
			// checkBoxShowScalarData
			// 
			this.checkBoxShowScalarData.AutoSize = true;
			this.checkBoxShowScalarData.Location = new System.Drawing.Point(6, 6);
			this.checkBoxShowScalarData.Name = "checkBoxShowScalarData";
			this.checkBoxShowScalarData.Size = new System.Drawing.Size(108, 17);
			this.checkBoxShowScalarData.TabIndex = 13;
			this.checkBoxShowScalarData.Text = "Show scalar data";
			this.checkBoxShowScalarData.UseVisualStyleBackColor = true;
			this.checkBoxShowScalarData.CheckedChanged += new System.EventHandler(this.checkBoxShowScalarData_CheckedChanged);
			// 
			// colorScaleSetter
			// 
			this.colorScaleSetter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.colorScaleSetter.DataVisualizer = null;
			this.colorScaleSetter.Location = new System.Drawing.Point(3, 247);
			this.colorScaleSetter.Name = "colorScaleSetter";
			this.colorScaleSetter.Size = new System.Drawing.Size(279, 142);
			this.colorScaleSetter.TabIndex = 12;
			// 
			// dataIndexSetterScalars
			// 
			this.dataIndexSetterScalars.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataIndexSetterScalars.DataFilter = MeshEditor.DataVisualizer.UI.DataIndexSetter.DataFilterOptions.None;
			this.dataIndexSetterScalars.DataVisualizer = null;
			this.dataIndexSetterScalars.Location = new System.Drawing.Point(3, 29);
			this.dataIndexSetterScalars.Name = "dataIndexSetterScalars";
			this.dataIndexSetterScalars.ShowVectorMagnitudes = false;
			this.dataIndexSetterScalars.Size = new System.Drawing.Size(279, 123);
			this.dataIndexSetterScalars.TabIndex = 0;
			// 
			// tabPageVectors
			// 
			this.tabPageVectors.Controls.Add(this.checkBoxMoveEndOfArrowsToNodes);
			this.tabPageVectors.Controls.Add(this.textBoxVectorLengthFactor);
			this.tabPageVectors.Controls.Add(this.label2);
			this.tabPageVectors.Controls.Add(this.checkBoxShowVectorData);
			this.tabPageVectors.Controls.Add(this.dataIndexSetterVectors);
			this.tabPageVectors.Location = new System.Drawing.Point(4, 22);
			this.tabPageVectors.Name = "tabPageVectors";
			this.tabPageVectors.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageVectors.Size = new System.Drawing.Size(285, 389);
			this.tabPageVectors.TabIndex = 1;
			this.tabPageVectors.Text = "Vectors";
			this.tabPageVectors.UseVisualStyleBackColor = true;
			// 
			// checkBoxMoveEndOfArrowsToNodes
			// 
			this.checkBoxMoveEndOfArrowsToNodes.AutoSize = true;
			this.checkBoxMoveEndOfArrowsToNodes.Location = new System.Drawing.Point(6, 200);
			this.checkBoxMoveEndOfArrowsToNodes.Name = "checkBoxMoveEndOfArrowsToNodes";
			this.checkBoxMoveEndOfArrowsToNodes.Size = new System.Drawing.Size(164, 17);
			this.checkBoxMoveEndOfArrowsToNodes.TabIndex = 5;
			this.checkBoxMoveEndOfArrowsToNodes.Text = "Move end of arrows to nodes";
			this.checkBoxMoveEndOfArrowsToNodes.UseVisualStyleBackColor = true;
			this.checkBoxMoveEndOfArrowsToNodes.CheckedChanged += new System.EventHandler(this.checkBoxMoveEndOfArrowsToNodes_CheckedChanged);
			// 
			// textBoxVectorLengthFactor
			// 
			this.textBoxVectorLengthFactor.Location = new System.Drawing.Point(82, 174);
			this.textBoxVectorLengthFactor.Name = "textBoxVectorLengthFactor";
			this.textBoxVectorLengthFactor.Size = new System.Drawing.Size(67, 20);
			this.textBoxVectorLengthFactor.TabIndex = 4;
			this.textBoxVectorLengthFactor.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxVectorLengthFactor_KeyPress);
			this.textBoxVectorLengthFactor.Leave += new System.EventHandler(this.textBoxVectorLengthFactor_Leave);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 177);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(70, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Length factor";
			// 
			// checkBoxShowVectorData
			// 
			this.checkBoxShowVectorData.AutoSize = true;
			this.checkBoxShowVectorData.Location = new System.Drawing.Point(6, 6);
			this.checkBoxShowVectorData.Name = "checkBoxShowVectorData";
			this.checkBoxShowVectorData.Size = new System.Drawing.Size(110, 17);
			this.checkBoxShowVectorData.TabIndex = 1;
			this.checkBoxShowVectorData.Text = "Show vector data";
			this.checkBoxShowVectorData.UseVisualStyleBackColor = true;
			this.checkBoxShowVectorData.CheckedChanged += new System.EventHandler(this.checkBoxShowVectorData_CheckedChanged);
			// 
			// dataIndexSetterVectors
			// 
			this.dataIndexSetterVectors.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataIndexSetterVectors.DataFilter = MeshEditor.DataVisualizer.UI.DataIndexSetter.DataFilterOptions.None;
			this.dataIndexSetterVectors.DataVisualizer = null;
			this.dataIndexSetterVectors.Location = new System.Drawing.Point(3, 29);
			this.dataIndexSetterVectors.Name = "dataIndexSetterVectors";
			this.dataIndexSetterVectors.ShowVectorMagnitudes = false;
			this.dataIndexSetterVectors.Size = new System.Drawing.Size(279, 145);
			this.dataIndexSetterVectors.TabIndex = 0;
			// 
			// tabPageDeformations
			// 
			this.tabPageDeformations.Controls.Add(this.groupBoxScale);
			this.tabPageDeformations.Controls.Add(this.checkBoxDrawDeformed);
			this.tabPageDeformations.Controls.Add(this.dataIndexSetterDeformations);
			this.tabPageDeformations.Location = new System.Drawing.Point(4, 22);
			this.tabPageDeformations.Name = "tabPageDeformations";
			this.tabPageDeformations.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageDeformations.Size = new System.Drawing.Size(285, 389);
			this.tabPageDeformations.TabIndex = 2;
			this.tabPageDeformations.Text = "Deformations";
			this.tabPageDeformations.UseVisualStyleBackColor = true;
			// 
			// groupBoxScale
			// 
			this.groupBoxScale.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxScale.Controls.Add(this.labelRelativeScale);
			this.groupBoxScale.Controls.Add(this.textBoxScaleValue);
			this.groupBoxScale.Controls.Add(this.label1);
			this.groupBoxScale.Controls.Add(this.radioButtonRelative);
			this.groupBoxScale.Controls.Add(this.radioButtonAbsolute);
			this.groupBoxScale.Controls.Add(this.trackBarDeformationMultiplier);
			this.groupBoxScale.Location = new System.Drawing.Point(6, 163);
			this.groupBoxScale.Name = "groupBoxScale";
			this.groupBoxScale.Size = new System.Drawing.Size(273, 112);
			this.groupBoxScale.TabIndex = 13;
			this.groupBoxScale.TabStop = false;
			this.groupBoxScale.Text = "Scale";
			// 
			// labelRelativeScale
			// 
			this.labelRelativeScale.AutoSize = true;
			this.labelRelativeScale.Location = new System.Drawing.Point(46, 46);
			this.labelRelativeScale.Name = "labelRelativeScale";
			this.labelRelativeScale.Size = new System.Drawing.Size(21, 13);
			this.labelRelativeScale.TabIndex = 16;
			this.labelRelativeScale.Text = "0%";
			// 
			// textBoxScaleValue
			// 
			this.textBoxScaleValue.Location = new System.Drawing.Point(6, 62);
			this.textBoxScaleValue.Name = "textBoxScaleValue";
			this.textBoxScaleValue.Size = new System.Drawing.Size(100, 20);
			this.textBoxScaleValue.TabIndex = 15;
			this.textBoxScaleValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxScaleValue_KeyDown);
			this.textBoxScaleValue.Leave += new System.EventHandler(this.textBoxScaleValue_Leave);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 46);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(37, 13);
			this.label1.TabIndex = 14;
			this.label1.Text = "Value:";
			// 
			// radioButtonRelative
			// 
			this.radioButtonRelative.AutoSize = true;
			this.radioButtonRelative.Checked = true;
			this.radioButtonRelative.Location = new System.Drawing.Point(85, 19);
			this.radioButtonRelative.Name = "radioButtonRelative";
			this.radioButtonRelative.Size = new System.Drawing.Size(64, 17);
			this.radioButtonRelative.TabIndex = 13;
			this.radioButtonRelative.TabStop = true;
			this.radioButtonRelative.Text = "Relative";
			this.radioButtonRelative.UseVisualStyleBackColor = true;
			this.radioButtonRelative.CheckedChanged += new System.EventHandler(this.radioButtonRelative_CheckedChanged);
			// 
			// radioButtonAbsolute
			// 
			this.radioButtonAbsolute.AutoSize = true;
			this.radioButtonAbsolute.Location = new System.Drawing.Point(6, 19);
			this.radioButtonAbsolute.Name = "radioButtonAbsolute";
			this.radioButtonAbsolute.Size = new System.Drawing.Size(66, 17);
			this.radioButtonAbsolute.TabIndex = 12;
			this.radioButtonAbsolute.Text = "Absolute";
			this.radioButtonAbsolute.UseVisualStyleBackColor = true;
			this.radioButtonAbsolute.CheckedChanged += new System.EventHandler(this.radioButtonAbsolute_CheckedChanged);
			// 
			// trackBarDeformationMultiplier
			// 
			this.trackBarDeformationMultiplier.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBarDeformationMultiplier.BackColor = System.Drawing.Color.White;
			this.trackBarDeformationMultiplier.Location = new System.Drawing.Point(6, 62);
			this.trackBarDeformationMultiplier.Maximum = 50;
			this.trackBarDeformationMultiplier.Name = "trackBarDeformationMultiplier";
			this.trackBarDeformationMultiplier.Size = new System.Drawing.Size(261, 45);
			this.trackBarDeformationMultiplier.TabIndex = 11;
			this.trackBarDeformationMultiplier.Scroll += new System.EventHandler(this.trackBarDeformationMultiplier_Scroll);
			// 
			// checkBoxDrawDeformed
			// 
			this.checkBoxDrawDeformed.AutoSize = true;
			this.checkBoxDrawDeformed.Location = new System.Drawing.Point(6, 6);
			this.checkBoxDrawDeformed.Name = "checkBoxDrawDeformed";
			this.checkBoxDrawDeformed.Size = new System.Drawing.Size(114, 17);
			this.checkBoxDrawDeformed.TabIndex = 10;
			this.checkBoxDrawDeformed.Text = "Draw deformations";
			this.checkBoxDrawDeformed.UseVisualStyleBackColor = true;
			this.checkBoxDrawDeformed.CheckedChanged += new System.EventHandler(this.checkBoxDrawDeformed_CheckedChanged);
			// 
			// dataIndexSetterDeformations
			// 
			this.dataIndexSetterDeformations.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataIndexSetterDeformations.DataFilter = MeshEditor.DataVisualizer.UI.DataIndexSetter.DataFilterOptions.None;
			this.dataIndexSetterDeformations.DataVisualizer = null;
			this.dataIndexSetterDeformations.Location = new System.Drawing.Point(3, 29);
			this.dataIndexSetterDeformations.Name = "dataIndexSetterDeformations";
			this.dataIndexSetterDeformations.ShowVectorMagnitudes = false;
			this.dataIndexSetterDeformations.Size = new System.Drawing.Size(279, 128);
			this.dataIndexSetterDeformations.TabIndex = 12;
			// 
			// DataPickerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tabControlDisplayStyles);
			this.Name = "DataPickerControl";
			this.Size = new System.Drawing.Size(293, 415);
			this.tabControlDisplayStyles.ResumeLayout(false);
			this.tabPageScalars.ResumeLayout(false);
			this.tabPageScalars.PerformLayout();
			this.tabPageVectors.ResumeLayout(false);
			this.tabPageVectors.PerformLayout();
			this.tabPageDeformations.ResumeLayout(false);
			this.tabPageDeformations.PerformLayout();
			this.groupBoxScale.ResumeLayout(false);
			this.groupBoxScale.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarDeformationMultiplier)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboBoxColorScaleType;
		private System.Windows.Forms.CheckBox checkBoxDrawIsoAreas;
		private System.Windows.Forms.ComboBox comboBoxIsoAreasSubIntervalNumber;
		private System.Windows.Forms.TabControl tabControlDisplayStyles;
		private System.Windows.Forms.TabPage tabPageScalars;
		private System.Windows.Forms.TabPage tabPageVectors;
		private System.Windows.Forms.TabPage tabPageDeformations;
		private System.Windows.Forms.TrackBar trackBarDeformationMultiplier;
		private System.Windows.Forms.CheckBox checkBoxDrawDeformed;
		private ColorScaleSetter colorScaleSetter;
		private DataIndexSetter dataIndexSetterScalars;
		private DataIndexSetter dataIndexSetterVectors;
		private DataIndexSetter dataIndexSetterDeformations;
		private System.Windows.Forms.CheckBox checkBoxShowScalarData;
		private System.Windows.Forms.CheckBox checkBoxShowVectorData;
		private System.Windows.Forms.Label labelSubintervals;
		private System.Windows.Forms.CheckBox checkBoxShowLegend;
		private System.Windows.Forms.ComboBox comboBoxDisplayMethod;
		private System.Windows.Forms.Label labelDisplayMethod;
		private System.Windows.Forms.GroupBox groupBoxScale;
		private System.Windows.Forms.RadioButton radioButtonRelative;
		private System.Windows.Forms.RadioButton radioButtonAbsolute;
		private System.Windows.Forms.Label labelRelativeScale;
		private System.Windows.Forms.TextBox textBoxScaleValue;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxVectorLengthFactor;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox checkBoxVectorMagnitudes;
		private System.Windows.Forms.CheckBox checkBoxMoveEndOfArrowsToNodes;

	}
}
