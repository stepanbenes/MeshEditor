using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MeshEditor.AvaloniaUI.Dialogs;

namespace MeshEditor.AvaloniaUI.Controls
{
	public class PropertyValueChangedEventArgs : EventArgs
	{
		public PropertyValueChangedEventArgs(PropertyDescriptor property, object oldValue)
		{
			Property = property;
			OldValue = oldValue;
		}

		public PropertyDescriptor Property { get; }
		public object OldValue { get; }
	}

	/// <summary>
	/// Editor of the public properties of an object, driven by the component model attributes
	/// (Category, DisplayName, Description, Browsable, ReadOnly) like System.Windows.Forms.PropertyGrid.
	/// Supports bool, enum, Color and any type with a TypeConverter from/to string.
	/// </summary>
	public class PropertyGrid : UserControl
	{
		private const string MiscCategory = "Misc";
		private const double NameColumnWidth = 180;

		private object selectedObject;
		private bool categorized = true;
		private readonly StackPanel rowsPanel = new StackPanel();
		private readonly TextBlock helpTitle = new TextBlock { FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap };
		private readonly TextBlock helpText = new TextBlock { TextWrapping = TextWrapping.Wrap };
		private readonly ToggleButton categorizedButton, alphabeticalButton;
		private readonly Dictionary<PropertyDescriptor, Action> refreshers = new Dictionary<PropertyDescriptor, Action>();
		private readonly HashSet<string> collapsedCategories = new HashSet<string>();
		private Border selectedRow;

		public event EventHandler<PropertyValueChangedEventArgs> PropertyValueChanged;

		public PropertyGrid()
		{
			categorizedButton = new ToggleButton { Content = "Categorized", IsChecked = true, Padding = new Thickness(6, 1), FontSize = 11 };
			alphabeticalButton = new ToggleButton { Content = "A-Z", Padding = new Thickness(6, 1), FontSize = 11 };
			categorizedButton.Click += (s, e) => setCategorized(true);
			alphabeticalButton.Click += (s, e) => setCategorized(false);
			ToolTip.SetTip(categorizedButton, "Categorized");
			ToolTip.SetTip(alphabeticalButton, "Alphabetical");

			var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(0, 0, 0, 4) };
			toolbar.Children.Add(categorizedButton);
			toolbar.Children.Add(alphabeticalButton);

			var help = new Border
			{
				BorderBrush = Brush.Parse("#ABADB3"),
				BorderThickness = new Thickness(1),
				Padding = new Thickness(6, 4),
				Margin = new Thickness(0, 4, 0, 0),
				MinHeight = 70,
				Child = new StackPanel { Children = { helpTitle, helpText } },
			};

			var list = new Border
			{
				BorderBrush = Brush.Parse("#ABADB3"),
				BorderThickness = new Thickness(1),
				Child = new ScrollViewer { Content = rowsPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled },
			};

			// dense like the WinForms PropertyGrid
			FontSize = 12;
			var root = new DockPanel();
			DockPanel.SetDock(toolbar, Dock.Top);
			DockPanel.SetDock(help, Dock.Bottom);
			root.Children.Add(toolbar);
			root.Children.Add(help);
			root.Children.Add(list);
			Content = root;
		}

		public object SelectedObject
		{
			get => selectedObject;
			set
			{
				selectedObject = value;
				rebuild();
			}
		}

		/// <summary>
		/// Displayed properties in display order (for tests).
		/// </summary>
		internal IReadOnlyList<PropertyDescriptor> DisplayedProperties { get; private set; } = Array.Empty<PropertyDescriptor>();

		/// <summary>
		/// Updates the shown values from the object.
		/// </summary>
		public void RefreshValues()
		{
			foreach (var refresh in refreshers.Values)
				refresh();
		}

		/// <summary>
		/// Sets a value like the user would (converts, assigns, raises PropertyValueChanged).
		/// </summary>
		public void SetPropertyValue(PropertyDescriptor property, object value)
		{
			object oldValue = property.GetValue(selectedObject);
			property.SetValue(selectedObject, value);
			if (refreshers.TryGetValue(property, out var refresh))
				refresh();
			PropertyValueChanged?.Invoke(this, new PropertyValueChangedEventArgs(property, oldValue));
		}

		private void setCategorized(bool value)
		{
			categorized = value;
			categorizedButton.IsChecked = value;
			alphabeticalButton.IsChecked = !value;
			rebuild();
		}

		private IEnumerable<PropertyDescriptor> browsableProperties()
		{
			return TypeDescriptor.GetProperties(selectedObject, new Attribute[] { BrowsableAttribute.Yes }).Cast<PropertyDescriptor>();
		}

		private static string categoryOf(PropertyDescriptor property) =>
			string.IsNullOrEmpty(property.Category) || property.Category == CategoryAttribute.Default.Category ? MiscCategory : property.Category;

		private void rebuild()
		{
			rowsPanel.Children.Clear();
			refreshers.Clear();
			selectedRow = null;
			helpTitle.Text = helpText.Text = string.Empty;
			if (selectedObject == null)
			{
				DisplayedProperties = Array.Empty<PropertyDescriptor>();
				return;
			}

			var properties = browsableProperties().ToList();
			var displayed = new List<PropertyDescriptor>();
			if (categorized)
			{
				// categories alphabetically, properties in declaration order (WinForms PropertySort.Categorized)
				foreach (var group in properties.GroupBy(categoryOf).OrderBy(g => g.Key, StringComparer.CurrentCulture))
				{
					var categoryRows = new StackPanel { IsVisible = !collapsedCategories.Contains(group.Key) };
					rowsPanel.Children.Add(createCategoryHeader(group.Key, categoryRows));
					foreach (var property in group)
					{
						categoryRows.Children.Add(createRow(property));
						displayed.Add(property);
					}
					rowsPanel.Children.Add(categoryRows);
				}
			}
			else
			{
				foreach (var property in properties.OrderBy(p => p.DisplayName, StringComparer.CurrentCulture))
				{
					rowsPanel.Children.Add(createRow(property));
					displayed.Add(property);
				}
			}
			DisplayedProperties = displayed;

			var first = displayed.FirstOrDefault();
			if (first != null)
				showHelp(first);
		}

		private Control createCategoryHeader(string category, Control categoryRows)
		{
			var chevron = new TextBlock { Text = categoryRows.IsVisible ? "⌄" : "›", Width = 16, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
			var header = new Border
			{
				Background = Brush.Parse("#F0F0F0"),
				Padding = new Thickness(0, 3),
				Cursor = new Cursor(StandardCursorType.Hand),
				Child = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Children = { chevron, new TextBlock { Text = category, FontWeight = FontWeight.Bold } },
				},
			};
			header.PointerPressed += (s, e) =>
			{
				categoryRows.IsVisible = !categoryRows.IsVisible;
				chevron.Text = categoryRows.IsVisible ? "⌄" : "›";
				if (categoryRows.IsVisible)
					collapsedCategories.Remove(category);
				else
					collapsedCategories.Add(category);
			};
			return header;
		}

		private Control createRow(PropertyDescriptor property)
		{
			var nameBlock = new TextBlock
			{
				Text = property.DisplayName,
				Margin = new Thickness(20, 0, 6, 0),
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
			};
			var editor = createEditor(property);
			var grid = new Grid { ColumnDefinitions = new ColumnDefinitions($"{NameColumnWidth},*"), MinHeight = 22 };
			grid.Children.Add(nameBlock);
			Grid.SetColumn(editor, 1);
			grid.Children.Add(editor);

			var row = new Border
			{
				BorderBrush = Brush.Parse("#F0F0F0"),
				BorderThickness = new Thickness(0, 0, 0, 1),
				Background = Brushes.Transparent,
				Child = grid,
				Tag = property,
			};
			row.AddHandler(PointerPressedEvent, (s, e) => selectRow(row, property), RoutingStrategies.Tunnel);
			row.AddHandler(GotFocusEvent, (s, e) => selectRow(row, property), RoutingStrategies.Bubble);
			return row;
		}

		private void selectRow(Border row, PropertyDescriptor property)
		{
			if (selectedRow != null)
				selectedRow.Background = Brushes.Transparent;
			selectedRow = row;
			row.Background = Brush.Parse("#CCE8FF");
			showHelp(property);
		}

		private void showHelp(PropertyDescriptor property)
		{
			helpTitle.Text = property.DisplayName;
			helpText.Text = property.Description ?? string.Empty;
		}

		#region Editors

		private Control createEditor(PropertyDescriptor property)
		{
			Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
			if (type == typeof(bool))
				return createBoolEditor(property);
			if (type.IsEnum && !property.IsReadOnly)
				return createEnumEditor(property, type);
			if (type == typeof(System.Drawing.Color))
				return createColorEditor(property);
			return createTextEditor(property);
		}

		private Control createBoolEditor(PropertyDescriptor property)
		{
			var checkBox = new CheckBox { IsEnabled = !property.IsReadOnly, VerticalAlignment = VerticalAlignment.Center };
			bool updating = false;
			void refresh()
			{
				updating = true;
				checkBox.IsChecked = (bool)property.GetValue(selectedObject);
				checkBox.Content = new TextBlock { Text = checkBox.IsChecked == true ? "True" : "False", FontWeight = FontWeight.Bold };
				updating = false;
			}
			checkBox.IsCheckedChanged += (s, e) =>
			{
				if (!updating)
					SetPropertyValue(property, checkBox.IsChecked == true);
			};
			refreshers[property] = refresh;
			refresh();
			return checkBox;
		}

		private Control createEnumEditor(PropertyDescriptor property, Type enumType)
		{
			var comboBox = new ComboBox
			{
				ItemsSource = Enum.GetNames(enumType),
				FontWeight = FontWeight.Bold,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				MinHeight = 20,
				Padding = new Thickness(4, 0),
			};
			bool updating = false;
			void refresh()
			{
				updating = true;
				comboBox.SelectedItem = property.GetValue(selectedObject)?.ToString();
				updating = false;
			}
			comboBox.SelectionChanged += (s, e) =>
			{
				if (!updating && comboBox.SelectedItem is string name)
					SetPropertyValue(property, Enum.Parse(enumType, name));
			};
			refreshers[property] = refresh;
			refresh();
			return comboBox;
		}

		private Control createColorEditor(PropertyDescriptor property)
		{
			var swatch = new Border { Width = 20, Height = 16, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Margin = new Thickness(2, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
			var textBox = createTextBox(property);
			var button = new Button { Content = "…", Padding = new Thickness(6, 0), MinHeight = 22, VerticalAlignment = VerticalAlignment.Center, IsEnabled = !property.IsReadOnly };
			ToolTip.SetTip(button, "Choose color");
			button.Click += async (s, e) =>
			{
				var current = (System.Drawing.Color)property.GetValue(selectedObject);
				var picker = new ColorPickerDialog { Color = ColorPickerDialog.FromDrawingColor(current) };
				if (await picker.ShowDialog(TopLevel.GetTopLevel(this) as Window))
					SetPropertyValue(property, ColorPickerDialog.ToDrawingColor(picker.Color));
			};

			Action refreshText = refreshers[property];
			void refresh()
			{
				refreshText();
				var color = (System.Drawing.Color)property.GetValue(selectedObject);
				swatch.Background = new SolidColorBrush(ColorPickerDialog.FromDrawingColor(color));
			}
			refreshers[property] = refresh;
			refresh();

			var panel = new DockPanel();
			DockPanel.SetDock(swatch, Dock.Left);
			DockPanel.SetDock(button, Dock.Right);
			panel.Children.Add(swatch);
			panel.Children.Add(button);
			panel.Children.Add(textBox);
			return panel;
		}

		private Control createTextEditor(PropertyDescriptor property) => createTextBox(property);

		/// <summary>
		/// Text box converting the value by the TypeConverter of the property (current culture, like the WinForms PropertyGrid).
		/// </summary>
		private TextBox createTextBox(PropertyDescriptor property)
		{
			var converter = property.Converter;
			var textBox = new TextBox
			{
				IsReadOnly = property.IsReadOnly || !converter.CanConvertFrom(typeof(string)),
				FontWeight = FontWeight.Bold,
				BorderThickness = new Thickness(0),
				Background = Brushes.Transparent,
				MinHeight = 20,
				Padding = new Thickness(2, 1),
				VerticalContentAlignment = VerticalAlignment.Center,
			};
			string shownText = null;
			void refresh()
			{
				shownText = converter.ConvertToString(null, CultureInfo.CurrentCulture, property.GetValue(selectedObject));
				textBox.Text = shownText;
			}

			async void commit()
			{
				if (textBox.IsReadOnly || textBox.Text == shownText)
					return;
				object value;
				try
				{
					value = converter.ConvertFromString(null, CultureInfo.CurrentCulture, textBox.Text);
				}
				catch (Exception ex)
				{
					string message = ex.InnerException?.Message ?? ex.Message;
					refresh();
					await MessageBox.Show("Property value is not valid." + Environment.NewLine + Environment.NewLine + message, "Properties Window", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
				try
				{
					SetPropertyValue(property, value);
				}
				catch (Exception ex)
				{
					refresh();
					await MessageBox.Show("Property value is not valid." + Environment.NewLine + Environment.NewLine + (ex.InnerException?.Message ?? ex.Message), "Properties Window", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}

			textBox.LostFocus += (s, e) => commit();
			textBox.KeyDown += (s, e) =>
			{
				if (e.Key == Key.Enter)
				{
					commit();
					e.Handled = true;
				}
				else if (e.Key == Key.Escape && textBox.Text != shownText)
				{
					refresh();
					e.Handled = true;
				}
			};
			refreshers[property] = refresh;
			refresh();
			return textBox;
		}

		#endregion
	}
}
