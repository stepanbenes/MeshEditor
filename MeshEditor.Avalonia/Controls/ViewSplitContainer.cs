using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace MeshEditor.AvaloniaUI.Controls
{
	/// <summary>
	/// Two panels separated by a movable splitter (counterpart of WinForms SplitContainer used by the split window feature).
	/// </summary>
	public sealed class ViewSplitContainer : Grid
	{
		public const double SplitterSize = 4;

		/// <summary>
		/// Raised when the user finishes dragging the splitter.
		/// </summary>
		public event EventHandler SplitterMoved;

		public ViewSplitContainer(Orientation orientation)
		{
			Orientation = orientation;
			Panel1 = new Border();
			Panel2 = new Border();
			var splitter = new GridSplitter
			{
				Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
				ResizeDirection = orientation == Orientation.Vertical ? GridResizeDirection.Columns : GridResizeDirection.Rows,
				ResizeBehavior = GridResizeBehavior.PreviousAndNext,
				ShowsPreview = false,
			};
			splitter.DragCompleted += (s, e) => SplitterMoved?.Invoke(this, EventArgs.Empty);

			if (orientation == Orientation.Vertical) // panels side by side (like WinForms Orientation.Vertical)
			{
				ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 0 });
				ColumnDefinitions.Add(new ColumnDefinition(SplitterSize, GridUnitType.Pixel));
				ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 0 });
				SetColumn(Panel1, 0);
				SetColumn(splitter, 1);
				SetColumn(Panel2, 2);
				splitter.Cursor = new Cursor(StandardCursorType.SizeWestEast);
			}
			else // panels above each other
			{
				RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star) { MinHeight = 0 });
				RowDefinitions.Add(new RowDefinition(SplitterSize, GridUnitType.Pixel));
				RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star) { MinHeight = 0 });
				SetRow(Panel1, 0);
				SetRow(splitter, 1);
				SetRow(Panel2, 2);
				splitter.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
			}

			Children.Add(Panel1);
			Children.Add(splitter);
			Children.Add(Panel2);
		}

		public Orientation Orientation { get; }

		public Border Panel1 { get; }

		public Border Panel2 { get; }

		/// <summary>
		/// Size of the first panel along the split direction (SplitContainer.SplitterDistance).
		/// </summary>
		public double SplitterDistance => Orientation == Orientation.Vertical ? Panel1.Bounds.Width : Panel1.Bounds.Height;

		/// <summary>
		/// Size of the whole container along the split direction.
		/// </summary>
		public double Extent => Orientation == Orientation.Vertical ? Bounds.Width : Bounds.Height;
	}
}
