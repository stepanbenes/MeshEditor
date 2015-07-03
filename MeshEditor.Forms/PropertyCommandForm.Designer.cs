namespace MeshEditor.WinUI
{
	partial class PropertyCommandForm
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
			this.comboBoxPropertyType = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.dataGridViewVariables = new System.Windows.Forms.DataGridView();
			this.ColumnVariableName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.label3 = new System.Windows.Forms.Label();
			this.labelCommandPattern = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.labelFilledPattern = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.labelResultText = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.labelPropertyNumber = new System.Windows.Forms.Label();
			this.groupBoxCommandDescription = new System.Windows.Forms.GroupBox();
			this.buttonAddCommand = new System.Windows.Forms.Button();
			this.buttonRemoveCommand = new System.Windows.Forms.Button();
			this.comboBoxAllCommands = new System.Windows.Forms.ComboBox();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewVariables)).BeginInit();
			this.groupBoxCommandDescription.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.Location = new System.Drawing.Point(726, 507);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(645, 507);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// comboBoxPropertyType
			// 
			this.comboBoxPropertyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxPropertyType.FormattingEnabled = true;
			this.comboBoxPropertyType.Location = new System.Drawing.Point(9, 37);
			this.comboBoxPropertyType.Name = "comboBoxPropertyType";
			this.comboBoxPropertyType.Size = new System.Drawing.Size(133, 21);
			this.comboBoxPropertyType.TabIndex = 4;
			this.comboBoxPropertyType.SelectedIndexChanged += new System.EventHandler(this.comboBoxPropertyType_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 21);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(31, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Type";
			// 
			// dataGridViewVariables
			// 
			this.dataGridViewVariables.AllowUserToAddRows = false;
			this.dataGridViewVariables.AllowUserToDeleteRows = false;
			this.dataGridViewVariables.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.dataGridViewVariables.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewVariables.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnVariableName,
            this.ColumnValue});
			this.dataGridViewVariables.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.dataGridViewVariables.Location = new System.Drawing.Point(9, 104);
			this.dataGridViewVariables.MultiSelect = false;
			this.dataGridViewVariables.Name = "dataGridViewVariables";
			this.dataGridViewVariables.RowHeadersVisible = false;
			this.dataGridViewVariables.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.dataGridViewVariables.Size = new System.Drawing.Size(381, 247);
			this.dataGridViewVariables.TabIndex = 6;
			this.dataGridViewVariables.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewVariables_CellEndEdit);
			// 
			// ColumnVariableName
			// 
			this.ColumnVariableName.HeaderText = "Variable";
			this.ColumnVariableName.Name = "ColumnVariableName";
			this.ColumnVariableName.ReadOnly = true;
			// 
			// ColumnValue
			// 
			this.ColumnValue.HeaderText = "Value";
			this.ColumnValue.Name = "ColumnValue";
			this.ColumnValue.Width = 250;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 70);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(93, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "Command pattern:";
			// 
			// labelCommandPattern
			// 
			this.labelCommandPattern.AutoSize = true;
			this.labelCommandPattern.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelCommandPattern.Location = new System.Drawing.Point(6, 85);
			this.labelCommandPattern.Name = "labelCommandPattern";
			this.labelCommandPattern.Size = new System.Drawing.Size(115, 13);
			this.labelCommandPattern.TabIndex = 8;
			this.labelCommandPattern.Text = "Description pattern";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(396, 104);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(117, 13);
			this.label5.TabIndex = 9;
			this.label5.Text = "Filled pattern (Invisible):";
			this.label5.Visible = false;
			// 
			// labelFilledPattern
			// 
			this.labelFilledPattern.AutoSize = true;
			this.labelFilledPattern.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelFilledPattern.Location = new System.Drawing.Point(396, 119);
			this.labelFilledPattern.Name = "labelFilledPattern";
			this.labelFilledPattern.Size = new System.Drawing.Size(81, 13);
			this.labelFilledPattern.TabIndex = 10;
			this.labelFilledPattern.Text = "Filled pattern";
			this.labelFilledPattern.Visible = false;
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 357);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(60, 13);
			this.label4.TabIndex = 11;
			this.label4.Text = "Result text:";
			// 
			// labelResultText
			// 
			this.labelResultText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelResultText.AutoSize = true;
			this.labelResultText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelResultText.Location = new System.Drawing.Point(6, 372);
			this.labelResultText.Name = "labelResultText";
			this.labelResultText.Size = new System.Drawing.Size(68, 13);
			this.labelResultText.TabIndex = 12;
			this.labelResultText.Text = "Result text";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(18, 9);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(118, 13);
			this.label6.TabIndex = 13;
			this.label6.Text = "Commands for property:";
			// 
			// labelPropertyNumber
			// 
			this.labelPropertyNumber.AutoSize = true;
			this.labelPropertyNumber.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelPropertyNumber.Location = new System.Drawing.Point(142, 9);
			this.labelPropertyNumber.Name = "labelPropertyNumber";
			this.labelPropertyNumber.Size = new System.Drawing.Size(14, 13);
			this.labelPropertyNumber.TabIndex = 14;
			this.labelPropertyNumber.Text = "0";
			// 
			// groupBoxCommandDescription
			// 
			this.groupBoxCommandDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxCommandDescription.Controls.Add(this.label3);
			this.groupBoxCommandDescription.Controls.Add(this.comboBoxPropertyType);
			this.groupBoxCommandDescription.Controls.Add(this.label2);
			this.groupBoxCommandDescription.Controls.Add(this.dataGridViewVariables);
			this.groupBoxCommandDescription.Controls.Add(this.labelResultText);
			this.groupBoxCommandDescription.Controls.Add(this.labelCommandPattern);
			this.groupBoxCommandDescription.Controls.Add(this.label4);
			this.groupBoxCommandDescription.Controls.Add(this.label5);
			this.groupBoxCommandDescription.Controls.Add(this.labelFilledPattern);
			this.groupBoxCommandDescription.Location = new System.Drawing.Point(12, 101);
			this.groupBoxCommandDescription.Name = "groupBoxCommandDescription";
			this.groupBoxCommandDescription.Size = new System.Drawing.Size(789, 400);
			this.groupBoxCommandDescription.TabIndex = 16;
			this.groupBoxCommandDescription.TabStop = false;
			this.groupBoxCommandDescription.Text = "Command description";
			// 
			// buttonAddCommand
			// 
			this.buttonAddCommand.Location = new System.Drawing.Point(21, 65);
			this.buttonAddCommand.Name = "buttonAddCommand";
			this.buttonAddCommand.Size = new System.Drawing.Size(133, 23);
			this.buttonAddCommand.TabIndex = 17;
			this.buttonAddCommand.Text = "Add command";
			this.buttonAddCommand.UseVisualStyleBackColor = true;
			this.buttonAddCommand.Click += new System.EventHandler(this.buttonAddCommand_Click);
			// 
			// buttonRemoveCommand
			// 
			this.buttonRemoveCommand.Location = new System.Drawing.Point(160, 65);
			this.buttonRemoveCommand.Name = "buttonRemoveCommand";
			this.buttonRemoveCommand.Size = new System.Drawing.Size(133, 23);
			this.buttonRemoveCommand.TabIndex = 18;
			this.buttonRemoveCommand.Text = "Remove command";
			this.buttonRemoveCommand.UseVisualStyleBackColor = true;
			this.buttonRemoveCommand.Click += new System.EventHandler(this.buttonRemoveCommand_Click);
			// 
			// comboBoxAllCommands
			// 
			this.comboBoxAllCommands.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxAllCommands.FormattingEnabled = true;
			this.comboBoxAllCommands.Location = new System.Drawing.Point(21, 38);
			this.comboBoxAllCommands.Name = "comboBoxAllCommands";
			this.comboBoxAllCommands.Size = new System.Drawing.Size(133, 21);
			this.comboBoxAllCommands.TabIndex = 19;
			this.comboBoxAllCommands.SelectedIndexChanged += new System.EventHandler(this.comboBoxAllCommands_SelectedIndexChanged);
			// 
			// PropertyCommandForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(813, 542);
			this.Controls.Add(this.comboBoxAllCommands);
			this.Controls.Add(this.buttonRemoveCommand);
			this.Controls.Add(this.buttonAddCommand);
			this.Controls.Add(this.groupBoxCommandDescription);
			this.Controls.Add(this.labelPropertyNumber);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PropertyCommandForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Preprocessor commands";
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewVariables)).EndInit();
			this.groupBoxCommandDescription.ResumeLayout(false);
			this.groupBoxCommandDescription.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.ComboBox comboBoxPropertyType;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.DataGridView dataGridViewVariables;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label labelCommandPattern;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label labelFilledPattern;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label labelResultText;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label labelPropertyNumber;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnVariableName;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnValue;
		private System.Windows.Forms.GroupBox groupBoxCommandDescription;
		private System.Windows.Forms.Button buttonAddCommand;
		private System.Windows.Forms.Button buttonRemoveCommand;
		private System.Windows.Forms.ComboBox comboBoxAllCommands;

	}
}