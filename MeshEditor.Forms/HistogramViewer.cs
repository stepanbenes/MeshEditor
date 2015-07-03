using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MeshEditor.Graphics;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// ovladaci prvek urceny pro zobrazeni histogramu cili cetnosti vyskytu hodnot nejake veliciny
	/// </summary>
	public partial class HistogramViewer : UserControl
	{
		private Histogram histogram;

		public static readonly Brush ColumnsBrush = Brushes.Blue;

		private const int DISTANCE_BETWEEN_COLUMNS = 1; // pixels

		private int dataWidth; // pixels

		public event EventHandler DataWidthChanged;

		private int firstLimit, secondLimit;

		public int FirstLimit
		{
			get { return firstLimit; }
			set { firstLimit = value; this.Invalidate(); }
		}

		public int SecondLimit
		{
			get { return secondLimit; }
			set { secondLimit = value; this.Invalidate(); }
		}

		public int DataWidth
		{
			get { return dataWidth; }
		}

		public HistogramViewer()
		{
			InitializeComponent();

			this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

			this.dataWidth = this.Width;
			this.histogram = null;
			firstLimit = secondLimit = 0;
		}

		public void SetHistogram(Histogram histogram)
		{
			this.histogram = histogram;
			this.Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (this.histogram == null)
				return;

			int distanceBetween = DISTANCE_BETWEEN_COLUMNS;
			int columnWidth = (this.Width - histogram.Columns.Length * DISTANCE_BETWEEN_COLUMNS) / histogram.Columns.Length;
			if (columnWidth <= 0)
			{
				columnWidth = 1;
				distanceBetween = 0;
			}

			
			//Font font = new Font(FontFamily.GenericSansSerif, 4f);

			float maximum;
			float[] columns = getColumHeights(out maximum);

			float heightFactor = 1f;
			if (maximum > 0f)
				heightFactor = (float)this.Height / maximum;

			for (int i = 0; i < columns.Length; i++)
			{
				int x = i * (columnWidth + distanceBetween) + 1;
				int height = (int)(columns[i] * heightFactor);

				e.Graphics.FillRectangle(ColumnsBrush, x, this.Height - height, columnWidth, height);
				//e.Graphics.DrawString(histogram.Columns[i].ToString(), font, Brushes.Black, x, this.Height);
			}

			// -------------------------------
			// limits
			Pen firstPen = new Pen(Color.Gray, 1f);
			Pen secondPen = new Pen(Color.Black, 1f);
			firstPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
			secondPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
			int firstLimitX = firstLimit * (columnWidth + distanceBetween);
			int secondLimitX = secondLimit * (columnWidth + distanceBetween);
			e.Graphics.DrawLine(firstPen, firstLimitX, 0, firstLimitX, this.Height);
			e.Graphics.DrawLine(secondPen, secondLimitX, 0, secondLimitX, this.Height);
			// -------------------------------

			this.dataWidth = (columns.Length - 1) * (columnWidth + distanceBetween);
			if (DataWidthChanged != null)
				DataWidthChanged(this, EventArgs.Empty);
		}

		private float[] getColumHeights(out float maximum)
		{
			float[] result = new float[histogram.Columns.Length];
			maximum = float.MinValue;

			for (int i = 0; i < histogram.Columns.Length; i++)
			{
				result[i] = (histogram.Columns[i] <= 1) ? 0f : (float)Math.Log10(histogram.Columns[i]);
				if (result[i] > maximum)
					maximum = result[i];
			}

			return result;
		}
	}
}
