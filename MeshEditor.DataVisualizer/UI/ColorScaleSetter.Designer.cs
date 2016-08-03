namespace MeshEditor.DataVisualizer.UI
{
	partial class ColorScaleSetter
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.dataGridViewControlPoints = new System.Windows.Forms.DataGridView();
			this.IsFixedColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.ValuesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColorColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewControlPoints)).BeginInit();
			this.SuspendLayout();
			// 
			// dataGridViewControlPoints
			// 
			this.dataGridViewControlPoints.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataGridViewControlPoints.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewControlPoints.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.IsFixedColumn,
            this.ValuesColumn,
            this.ColorColumn});
			this.dataGridViewControlPoints.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.dataGridViewControlPoints.Location = new System.Drawing.Point(3, 3);
			this.dataGridViewControlPoints.Name = "dataGridViewControlPoints";
			this.dataGridViewControlPoints.RowHeadersVisible = false;
			this.dataGridViewControlPoints.Size = new System.Drawing.Size(272, 245);
			this.dataGridViewControlPoints.TabIndex = 0;
			this.dataGridViewControlPoints.CellParsing += new System.Windows.Forms.DataGridViewCellParsingEventHandler(this.dataGridViewControlPoints_CellParsing);
			this.dataGridViewControlPoints.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewControlPoints_CellValueChanged);
			this.dataGridViewControlPoints.ColumnAdded += new System.Windows.Forms.DataGridViewColumnEventHandler(this.dataGridViewControlPoints_ColumnAdded);
			this.dataGridViewControlPoints.CurrentCellDirtyStateChanged += new System.EventHandler(this.dataGridViewControlPoints_CurrentCellDirtyStateChanged);
			// 
			// IsFixedColumn
			// 
			this.IsFixedColumn.DataPropertyName = "IsFixed";
			this.IsFixedColumn.HeaderText = "IsFixed";
			this.IsFixedColumn.Name = "IsFixedColumn";
			this.IsFixedColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.IsFixedColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.IsFixedColumn.Width = 60;
			// 
			// ValuesColumn
			// 
			this.ValuesColumn.DataPropertyName = "Value";
			dataGridViewCellStyle1.Format = "G4";
			dataGridViewCellStyle1.NullValue = null;
			this.ValuesColumn.DefaultCellStyle = dataGridViewCellStyle1;
			this.ValuesColumn.HeaderText = "Value";
			this.ValuesColumn.Name = "ValuesColumn";
			// 
			// ColorColumn
			// 
			this.ColorColumn.DataPropertyName = "Color";
			dataGridViewCellStyle2.Format = "X";
			dataGridViewCellStyle2.NullValue = null;
			this.ColorColumn.DefaultCellStyle = dataGridViewCellStyle2;
			this.ColorColumn.HeaderText = "Color";
			this.ColorColumn.Name = "ColorColumn";
			this.ColorColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			// 
			// ColorScaleSetter
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.dataGridViewControlPoints);
			this.Name = "ColorScaleSetter";
			this.Size = new System.Drawing.Size(278, 251);
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewControlPoints)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView dataGridViewControlPoints;
		private System.Windows.Forms.DataGridViewCheckBoxColumn IsFixedColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ValuesColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColorColumn;
	}
}
