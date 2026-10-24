using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.Data;

namespace MeshEditor.AvaloniaUI.Controls
{
	/// <summary>
	/// Property number with its color; clicking the color opens a color dialog (port of MeshEditor.WinUI.PropertyColorControl).
	/// </summary>
	public class PropertyColorControl : Grid
	{
		private readonly Property property;
		private readonly TextBlock labelPropertyNumber;
		private readonly Border pictureBoxPropertyColor;
		private System.Drawing.Color color;

		public event EventHandler ColorChanged;

		public PropertyColorControl(Property property, System.Drawing.Color color)
		{
			this.property = property;
			ColumnDefinitions = new ColumnDefinitions("*,140");
			MinWidth = 200;
			Height = 22;
			Margin = new Thickness(3, 1);

			labelPropertyNumber = new TextBlock
			{
				Text = string.Format("Property {0}", property.Value),
				VerticalAlignment = VerticalAlignment.Center,
			};
			pictureBoxPropertyColor = new Border
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1),
				Height = 18,
				Cursor = new Cursor(StandardCursorType.Hand),
			};
			SetColumn(pictureBoxPropertyColor, 1);
			pictureBoxPropertyColor.PointerPressed += async (s, e) => await pictureBoxPropertyColor_Click();

			Children.Add(labelPropertyNumber);
			Children.Add(pictureBoxPropertyColor);
			setColor(color);
		}

		public Property Property => property;

		public System.Drawing.Color Color => color;

		internal Border ColorBox => pictureBoxPropertyColor;

		internal TextBlock Label => labelPropertyNumber;

		private void setColor(System.Drawing.Color newColor)
		{
			color = newColor;
			pictureBoxPropertyColor.Background = new SolidColorBrush(ColorPickerDialog.FromDrawingColor(newColor));
		}

		/// <summary>
		/// Lets the user pick a new color (also used by tests to simulate the click).
		/// </summary>
		internal async Task pictureBoxPropertyColor_Click()
		{
			var colorPicker = new ColorPickerDialog { Color = ColorPickerDialog.FromDrawingColor(color) };
			if (await colorPicker.ShowDialog(TopLevel.GetTopLevel(this) as Window))
			{
				setColor(ColorPickerDialog.ToDrawingColor(colorPicker.Color));
				ColorChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
