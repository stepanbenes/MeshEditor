using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MeshEditor.Graphics;

namespace MeshEditor.AvaloniaUI.Controls
{
	/// <summary>
	/// Shows a histogram (frequency of values) with two limit lines (port of MeshEditor.WinUI.HistogramViewer).
	/// </summary>
	public class HistogramViewer : Control
	{
		private Histogram histogram;

		public static readonly IBrush ColumnsBrush = Brushes.Blue;
		/// <summary>
		/// SystemColors.Info, the background of the WinForms control.
		/// </summary>
		private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xE1));
		private static readonly IPen firstPen = new Pen(Brushes.Gray, 1, DashStyle.Dash);
		private static readonly IPen secondPen = new Pen(Brushes.Black, 1, DashStyle.Dash);

		private const int DISTANCE_BETWEEN_COLUMNS = 1;

		private int firstLimit, secondLimit;

		public event EventHandler DataWidthChanged;

		public int FirstLimit
		{
			get => firstLimit;
			set { firstLimit = value; InvalidateVisual(); }
		}

		public int SecondLimit
		{
			get => secondLimit;
			set { secondLimit = value; InvalidateVisual(); }
		}

		public int DataWidth { get; private set; }

		public Histogram Histogram => histogram;

		public HistogramViewer()
		{
			MinWidth = 100;
			MinHeight = 50;
		}

		public void SetHistogram(Histogram histogram)
		{
			this.histogram = histogram;
			InvalidateVisual();
		}

		public override void Render(DrawingContext context)
		{
			base.Render(context);
			int width = (int)Bounds.Width, height = (int)Bounds.Height;
			context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));
			if (histogram == null || histogram.Columns.Length == 0)
				return;

			int distanceBetween = DISTANCE_BETWEEN_COLUMNS;
			int columnWidth = (width - histogram.Columns.Length * DISTANCE_BETWEEN_COLUMNS) / histogram.Columns.Length;
			if (columnWidth <= 0)
			{
				columnWidth = 1;
				distanceBetween = 0;
			}

			float[] columns = getColumnHeights(out float maximum);
			float heightFactor = 1f;
			if (maximum > 0f)
				heightFactor = height / maximum;

			for (int i = 0; i < columns.Length; i++)
			{
				int x = i * (columnWidth + distanceBetween) + 1;
				int columnHeight = (int)(columns[i] * heightFactor);
				context.FillRectangle(ColumnsBrush, new Rect(x, height - columnHeight, columnWidth, columnHeight));
			}

			// limits
			int firstLimitX = firstLimit * (columnWidth + distanceBetween);
			int secondLimitX = secondLimit * (columnWidth + distanceBetween);
			context.DrawLine(firstPen, new Point(firstLimitX + 0.5, 0), new Point(firstLimitX + 0.5, height));
			context.DrawLine(secondPen, new Point(secondLimitX + 0.5, 0), new Point(secondLimitX + 0.5, height));

			int dataWidth = (columns.Length - 1) * (columnWidth + distanceBetween);
			if (dataWidth != DataWidth)
			{
				DataWidth = dataWidth;
				DataWidthChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Logarithmic column heights (log10 of the count).
		/// </summary>
		private float[] getColumnHeights(out float maximum)
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
