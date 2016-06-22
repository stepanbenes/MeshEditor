using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class ColorScaleSetter : UserControl
	{
		IDataVisualizerController dataVisualizer;

		public ColorScaleSetter()
		{
			InitializeComponent();
		}

		public IDataVisualizerController DataVisualizer
		{
			get { return dataVisualizer; }
			set
			{
				if (dataVisualizer != value)
				{
					dataVisualizer = value;
					setupControlPoints();
				}
			}
		}

		public ColorScale ColorScale
		{
			get { return dataVisualizer != null ? dataVisualizer.Settings.ColorScale : null; }
		}

		private void setupControlPoints()
		{
			dataGridViewControlPoints.DataSource = null;
			if (dataVisualizer == null || ColorScale == null || ColorScale.ControlPoints == null)
				return;
			dataGridViewControlPoints.DataSource = ColorScale.ControlPoints.Reverse().ToArray(); // minimum on bottom, maximum on top
		}

		private void dataGridViewControlPoints_CurrentCellDirtyStateChanged(object sender, EventArgs e)
		{
			if (dataGridViewControlPoints.IsCurrentCellDirty && dataGridViewControlPoints.CurrentCell is DataGridViewCheckBoxCell)
			{
				dataGridViewControlPoints.CommitEdit(DataGridViewDataErrorContexts.Commit);
			}
		}

		private void dataGridViewControlPoints_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			dataGridViewControlPoints.Refresh();

		}

		private void dataGridViewControlPoints_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
		{
			switch (e.Column.HeaderText)
			{
				case "IsFixed":
					//e.Column.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
					e.Column.Width = 60;
					break;
				case "Value":
					e.Column.DefaultCellStyle.Format = "G4";
					break;
				case "Color":
					e.Column.DefaultCellStyle.Format = "X6";
					e.Column.ToolTipText = "Hexadecimal Blue Green Red format";
					//e.Column.HeaderText = e.Column.HeaderText + " (BGR)";
					break;
			}
		}

		private void dataGridViewControlPoints_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
		{
			if (!dataGridViewControlPoints.Columns[e.ColumnIndex].DefaultCellStyle.Format.StartsWith("X", StringComparison.InvariantCultureIgnoreCase))
			{
				e.ParsingApplied = true;
				return;
			}

			if (e.Value != null && e.DesiredType.Equals(typeof(int)))
			{
				try
				{
					/// Convert to a hex value
					e.Value = int.Parse((string)e.Value, System.Globalization.NumberStyles.AllowHexSpecifier);
					e.ParsingApplied = true;
				}
				catch
				{
					//MessageBox.Show("Input is not a hex value!");
				}
			}
		}

	}
}
