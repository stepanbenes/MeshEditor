using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace MeshEditor.AvaloniaUI.Controls
{
	/// <summary>
	/// Item of <see cref="CheckedListPanel"/> (ListViewItem with check box in the WinForms UI).
	/// </summary>
	public sealed class CheckedListItem : INotifyPropertyChanged
	{
		private bool isChecked;

		public CheckedListItem(object tag, bool isChecked, params string[] columns)
		{
			Tag = tag;
			this.isChecked = isChecked;
			Columns = columns;
		}

		public object Tag { get; }
		public string[] Columns { get; }
		public IBrush Background { get; init; } = Brushes.Transparent;
		public IBrush Foreground { get; init; } = Brushes.Black;

		public bool IsChecked
		{
			get => isChecked;
			set
			{
				if (isChecked != value)
				{
					isChecked = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
					CheckedChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		internal event EventHandler CheckedChanged;

		public event PropertyChangedEventHandler PropertyChanged;
	}

	/// <summary>
	/// Label, list with checkable items in columns, and "Check All" / "Check Selected" three-state check boxes
	/// (a tab of the WinForms ShowHideElementsForm: ListView with CheckBoxes and the two check boxes below it).
	/// </summary>
	public class CheckedListPanel : DockPanel
	{
		private readonly ListBox listBox;
		private readonly Grid header;
		private readonly double[] columnWidths;
		private bool checkAllStateChanging, checkSelectedStateChanging, itemCheckChanging;

		public CheckedListPanel(string label, params (string header, double width)[] columns)
		{
			columnWidths = columns.Select(c => c.width).ToArray();
			var labelBlock = new TextBlock { Text = label, Margin = new Thickness(0, 4, 0, 8) };
			DockPanel.SetDock(labelBlock, Dock.Top);

			CheckAll = new CheckBox { Content = "Check All", IsThreeState = true, Margin = new Thickness(0, 0, 16, 0) };
			CheckSelected = new CheckBox { Content = "Check Selected", IsThreeState = true };
			CheckAll.IsCheckedChanged += (s, e) => checkAll_CheckedChanged();
			CheckSelected.IsCheckedChanged += (s, e) => checkSelected_CheckedChanged();
			var bottom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { CheckAll, CheckSelected } };
			DockPanel.SetDock(bottom, Dock.Bottom);

			header = createColumnsGrid();
			header.Background = Brush.Parse("#F0F0F0");
			for (int i = 0; i < columns.Length; i++)
			{
				var text = new TextBlock { Text = columns[i].header, Margin = new Thickness(i == 0 ? 26 : 4, 3, 4, 3) };
				Grid.SetColumn(text, i);
				header.Children.Add(text);
			}
			DockPanel.SetDock(header, Dock.Top);

			listBox = new ListBox
			{
				ItemsSource = Items,
				SelectionMode = SelectionMode.Multiple,
				ItemTemplate = new FuncDataTemplate<CheckedListItem>((item, _) => createRow(item)),
			};
			// compact rows like the WinForms ListView
			listBox.Styles.Add(new Avalonia.Styling.Style(x => Avalonia.Styling.Selectors.OfType<ListBoxItem>(x))
			{
				Setters =
				{
					new Avalonia.Styling.Setter(ListBoxItem.PaddingProperty, new Thickness(2, 1)),
					new Avalonia.Styling.Setter(ListBoxItem.MinHeightProperty, 22.0),
				},
			});
			listBox.SelectionChanged += (s, e) =>
			{
				if (!itemCheckChanging)
					updateCheckSelectedState();
			};

			var listBorder = new Border
			{
				BorderBrush = Brush.Parse("#ABADB3"),
				BorderThickness = new Thickness(1),
				Child = new DockPanel { Children = { header, listBox } },
			};

			Children.Add(labelBlock);
			Children.Add(bottom);
			Children.Add(listBorder);

			Items.CollectionChanged += (s, e) =>
			{
				if (e.NewItems != null)
					foreach (CheckedListItem item in e.NewItems)
						item.CheckedChanged += item_CheckedChanged;
				updateCheckAllState();
				updateCheckSelectedState();
			};
		}

		public ObservableCollection<CheckedListItem> Items { get; } = new ObservableCollection<CheckedListItem>();

		public IEnumerable<CheckedListItem> CheckedItems => Items.Where(i => i.IsChecked);

		public System.Collections.IList SelectedItems => listBox.SelectedItems;

		public CheckBox CheckAll { get; }

		public CheckBox CheckSelected { get; }

		private Grid createColumnsGrid()
		{
			var grid = new Grid();
			foreach (double width in columnWidths)
				grid.ColumnDefinitions.Add(new ColumnDefinition(width, GridUnitType.Pixel));
			grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
			return grid;
		}

		private Control createRow(CheckedListItem item)
		{
			var grid = createColumnsGrid();
			var checkBox = new CheckBox { IsChecked = item.IsChecked, MinWidth = 0, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
			checkBox.IsCheckedChanged += (s, e) => item.IsChecked = checkBox.IsChecked == true;
			item.PropertyChanged += (s, e) => checkBox.IsChecked = item.IsChecked;

			var first = new StackPanel { Orientation = Orientation.Horizontal, Children = { checkBox } };
			for (int i = 0; i < item.Columns.Length && i < Math.Max(1, columnWidths.Length); i++)
			{
				var text = new TextBlock { Text = item.Columns[i], Foreground = item.Foreground, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(4, 0) };
				if (i == 0)
					first.Children.Add(text);
				else
				{
					Grid.SetColumn(text, i);
					grid.Children.Add(text);
				}
			}
			grid.Children.Add(first);
			return new Border { Background = item.Background, Padding = new Thickness(2, 0), Child = grid };
		}

		private void item_CheckedChanged(object sender, EventArgs e)
		{
			if (itemCheckChanging)
				return;
			updateCheckAllState();
			updateCheckSelectedState();
		}

		private void checkAll_CheckedChanged()
		{
			if (checkAllStateChanging)
				return;
			bool value = CheckAll.IsChecked == true;
			if (CheckAll.IsChecked == null) // clicking cycles through the indeterminate state; treat it as unchecked like WinForms
			{
				checkAllStateChanging = true;
				CheckAll.IsChecked = false;
				checkAllStateChanging = false;
				value = false;
			}
			itemCheckChanging = true;
			foreach (var item in Items)
				item.IsChecked = value;
			itemCheckChanging = false;
			updateCheckSelectedState();
		}

		private void checkSelected_CheckedChanged()
		{
			if (checkSelectedStateChanging)
				return;
			bool value = CheckSelected.IsChecked == true;
			if (CheckSelected.IsChecked == null)
			{
				checkSelectedStateChanging = true;
				CheckSelected.IsChecked = false;
				checkSelectedStateChanging = false;
				value = false;
			}
			itemCheckChanging = true;
			foreach (CheckedListItem item in listBox.SelectedItems.OfType<CheckedListItem>().ToList())
				item.IsChecked = value;
			itemCheckChanging = false;
			updateCheckAllState();
		}

		private static bool? state(IEnumerable<CheckedListItem> items)
		{
			bool none = true, all = true;
			foreach (var item in items)
			{
				if (item.IsChecked)
					none = false;
				else
					all = false;
			}
			return none ? false : all ? true : null;
		}

		private void updateCheckAllState()
		{
			checkAllStateChanging = true;
			CheckAll.IsChecked = state(Items);
			checkAllStateChanging = false;
		}

		private void updateCheckSelectedState()
		{
			checkSelectedStateChanging = true;
			CheckSelected.IsChecked = state(listBox.SelectedItems.OfType<CheckedListItem>());
			checkSelectedStateChanging = false;
		}
	}
}
