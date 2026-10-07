using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.DataVisualizer;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// Base of the visualizer settings dialogs: modal window with OK / Apply / Cancel buttons.
	/// </summary>
	public abstract class SettingsDialogBase : Window
	{
		protected readonly StackPanel ContentPanel = new StackPanel { Spacing = 6 };
		internal readonly Button buttonOK, buttonApply, buttonCancel;

		protected SettingsDialogBase(string title)
		{
			Title = title;
			CanMinimize = CanMaximize = false;
			ShowInTaskbar = false;
			SizeToContent = SizeToContent.WidthAndHeight;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			Button button(string text) => new Button { Content = text, MinWidth = 75, HorizontalContentAlignment = HorizontalAlignment.Center };
			buttonOK = button("OK");
			buttonApply = button("Apply");
			buttonCancel = button("Cancel");
			buttonOK.IsDefault = true;
			buttonCancel.IsCancel = true;
			buttonOK.Click += (s, e) => { OnOK(); Close(); };
			buttonApply.Click += (s, e) => OnApply();
			buttonCancel.Click += (s, e) => { OnCancel(); Close(); };

			var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Margin = new Thickness(0, 8, 0, 0), Children = { buttonOK, buttonApply, buttonCancel } };
			var root = new DockPanel { Margin = new Thickness(12), MinWidth = 320 };
			DockPanel.SetDock(buttons, Dock.Bottom);
			root.Children.Add(buttons);
			root.Children.Add(ContentPanel);
			Content = root;
		}

		protected abstract void OnOK();
		protected abstract void OnApply();
		protected abstract void OnCancel();

		public new async Task ShowDialog(Window owner)
		{
			owner ??= DialogOwner.Current;
			if (owner != null)
				await base.ShowDialog(owner);
			else
			{
				var closed = new TaskCompletionSource();
				Closed += (s, e) => closed.TrySetResult();
				WindowStartupLocation = WindowStartupLocation.CenterScreen;
				Show();
				await closed.Task;
			}
		}
	}

	/// <summary>
	/// Color scale type, control points and iso-areas (port of MeshEditor.DataVisualizer.UI.ColorScaleSettingsForm).
	/// </summary>
	public class ColorScaleSettingsDialog : SettingsDialogBase
	{
		private readonly IVisualizerSettings originalSettings, appliedSettings, currentSettings;
		private bool updatingView;

		internal readonly ComboBox comboBoxColorScaleType, comboBoxNumberOfSubIntervals;
		internal readonly CheckBox checkBoxShowIsoAreas;
		internal readonly StackPanel controlPointsPanel = new StackPanel { Spacing = 2 };

		public event EventHandler SettingsChanged;

		public ColorScaleSettingsDialog(IVisualizerSettings settings)
			: base("Color scale settings")
		{
			Debug.Assert(settings != null);
			comboBoxColorScaleType = new ComboBox { ItemsSource = Enum.GetValues(typeof(ColorScale.Types)).Cast<object>().ToList(), MinWidth = 160 };
			comboBoxNumberOfSubIntervals = new ComboBox { ItemsSource = Enumerable.Range(1, 5).Cast<object>().ToList(), MinWidth = 80 };
			checkBoxShowIsoAreas = new CheckBox { Content = "Show iso-areas" };

			ContentPanel.Children.Add(new TextBlock { Text = "Color scale type" });
			ContentPanel.Children.Add(comboBoxColorScaleType);
			ContentPanel.Children.Add(new TextBlock { Text = "Color scale control points", Margin = new Thickness(0, 6, 0, 0) });
			ContentPanel.Children.Add(new Border { BorderBrush = Brush.Parse("#ABADB3"), BorderThickness = new Thickness(1), Padding = new Thickness(4), Child = new ScrollViewer { MaxHeight = 320, Content = controlPointsPanel } });
			ContentPanel.Children.Add(checkBoxShowIsoAreas);
			ContentPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { new TextBlock { Text = "Number of sub-intervals", VerticalAlignment = VerticalAlignment.Center }, comboBoxNumberOfSubIntervals } });

			appliedSettings = settings;
			originalSettings = new VisualizerSettings();
			currentSettings = new VisualizerSettings();
			copyProperties(appliedSettings, originalSettings);
			copyProperties(appliedSettings, currentSettings);
			updateView();

			checkBoxShowIsoAreas.IsCheckedChanged += (s, e) => { if (!updatingView) currentSettings.DrawIsoAreas = checkBoxShowIsoAreas.IsChecked == true; };
			comboBoxNumberOfSubIntervals.SelectionChanged += (s, e) => { if (!updatingView && comboBoxNumberOfSubIntervals.SelectedItem is int n) currentSettings.IsoAreasSubIntervalNumber = n; };
			comboBoxColorScaleType.SelectionChanged += (s, e) =>
			{
				if (updatingView || comboBoxColorScaleType.SelectedItem == null)
					return;
				currentSettings.ColorScale.Type = (ColorScale.Types)comboBoxColorScaleType.SelectedItem;
				setupControlPoints();
			};
		}

		internal IVisualizerSettings CurrentSettings => currentSettings;

		protected override void OnClosed(EventArgs e)
		{
			removeAllControlPointSetters();
			base.OnClosed(e);
		}

		private void updateView()
		{
			try
			{
				updatingView = true;
				checkBoxShowIsoAreas.IsChecked = currentSettings.DrawIsoAreas;
				comboBoxNumberOfSubIntervals.SelectedItem = currentSettings.IsoAreasSubIntervalNumber;
				comboBoxColorScaleType.SelectedItem = currentSettings.ColorScale?.Type;
				setupControlPoints();
			}
			finally
			{
				updatingView = false;
			}
		}

		protected override void OnOK()
		{
			applySettings(currentSettings);
		}

		protected override void OnApply()
		{
			applySettings(currentSettings);
		}

		protected override void OnCancel()
		{
			applySettings(originalSettings);
		}

		private void applySettings(IVisualizerSettings source)
		{
			// the min/max values follow the data, which can be loaded while the dialog is open; keep the current ones
			double minValue = appliedSettings.ColorScale.MinValue, maxValue = appliedSettings.ColorScale.MaxValue;
			copyProperties(source, appliedSettings);
			appliedSettings.ColorScale.SetMinMaxValue(minValue, maxValue);
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void setupControlPoints()
		{
			removeAllControlPointSetters();
			foreach (var controlPoint in currentSettings.ColorScale.ControlPoints.Reverse())
				controlPointsPanel.Children.Add(new ColorScaleControlPointSetter(controlPoint));
		}

		private void removeAllControlPointSetters()
		{
			foreach (var control in controlPointsPanel.Children.OfType<ColorScaleControlPointSetter>())
				control.Detach();
			controlPointsPanel.Children.Clear();
		}

		private static void copyProperties(IVisualizerSettings source, IVisualizerSettings destination)
		{
			destination.ColorScale = new ColorScale(source.ColorScale); // clone color scale object
			destination.DrawIsoAreas = source.DrawIsoAreas;
			destination.IsoAreasSubIntervalNumber = source.IsoAreasSubIntervalNumber;
		}
	}

	/// <summary>
	/// One control point of the color scale: fixed flag, value and color (port of MeshEditor.DataVisualizer.UI.ColorScaleControlPointSetter).
	/// </summary>
	public class ColorScaleControlPointSetter : StackPanel
	{
		private readonly ColorScale.ControlPoint controlPoint;
		internal readonly CheckBox checkBoxIsFixed = new CheckBox { MinWidth = 0, VerticalAlignment = VerticalAlignment.Center };
		internal readonly TextBox textBoxValue = new TextBox { Width = 120 };
		internal readonly Border pictureBoxColor = new Border { Width = 100, Height = 20, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center };
		private bool updating;

		public ColorScaleControlPointSetter(ColorScale.ControlPoint controlPoint)
		{
			Orientation = Orientation.Horizontal;
			Spacing = 6;
			Children.Add(checkBoxIsFixed);
			Children.Add(textBoxValue);
			Children.Add(pictureBoxColor);

			this.controlPoint = controlPoint;
			this.controlPoint.PropertyChanged += controlPoint_PropertyChanged;
			updateValues();

			checkBoxIsFixed.IsCheckedChanged += (s, e) =>
			{
				if (updating)
					return;
				controlPoint.IsFixed = checkBoxIsFixed.IsChecked == true;
				textBoxValue.IsEnabled = controlPoint.IsFixed;
			};
			textBoxValue.TextChanged += (s, e) =>
			{
				if (!updating && double.TryParse(textBoxValue.Text, out double value))
					controlPoint.Value = value;
			};
			pictureBoxColor.PointerPressed += async (s, e) =>
			{
				var colorPicker = new ColorPickerDialog { Color = ColorPickerDialog.FromDrawingColor(Utilities.Functions.ColorFromRgba32(controlPoint.Color)) };
				if (await colorPicker.ShowDialog(TopLevel.GetTopLevel(this) as Window))
					controlPoint.Color = Utilities.Functions.ColorToRgba32(ColorPickerDialog.ToDrawingColor(colorPicker.Color));
			};
		}

		private void controlPoint_PropertyChanged(object s, PropertyChangedEventArgs e) => updateValues();

		private void updateValues()
		{
			try
			{
				updating = true;
				checkBoxIsFixed.IsChecked = controlPoint.IsFixed;
				if (!double.TryParse(textBoxValue.Text, out double shown) || shown != controlPoint.Value)
					textBoxValue.Text = controlPoint.Value.ToString();
				textBoxValue.IsEnabled = controlPoint.IsFixed;
				pictureBoxColor.Background = new SolidColorBrush(ColorPickerDialog.FromDrawingColor(Utilities.Functions.ColorFromRgba32(controlPoint.Color)));
			}
			finally
			{
				updating = false;
			}
		}

		public void Detach()
		{
			this.controlPoint.PropertyChanged -= controlPoint_PropertyChanged;
		}
	}

	/// <summary>
	/// Arrow length and direction of the vector field (port of MeshEditor.DataVisualizer.UI.VectorFieldVisualizationSettingsForm).
	/// </summary>
	public class VectorFieldVisualizationSettingsDialog : SettingsDialogBase
	{
		private readonly IVisualizerSettings originalSettings, appliedSettings, currentSettings;
		private bool isUpdatingArrowLengthView;

		internal readonly Slider trackBarVectorLengthFactor = new Slider { Minimum = 1, Maximum = 200, Value = 100, TickFrequency = 1, IsSnapToTickEnabled = true, MinWidth = 300 };
		internal readonly TextBox textBoxVectorLengthFactor = new TextBox { Width = 80 };
		internal readonly CheckBox checkBoxInvertVectorArrows = new CheckBox { Content = "Invert vector arrows" };
		internal readonly CheckBox checkBoxIsArrowLengthFixed = new CheckBox { Content = "Fix arrow length" };

		public event EventHandler SettingsChanged;

		public VectorFieldVisualizationSettingsDialog(IVisualizerSettings settings)
			: base("Vector field visualization settings")
		{
			Debug.Assert(settings != null);
			ContentPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { new TextBlock { Text = "Arrow length factor:", VerticalAlignment = VerticalAlignment.Center }, textBoxVectorLengthFactor } });
			ContentPanel.Children.Add(trackBarVectorLengthFactor);
			ContentPanel.Children.Add(checkBoxIsArrowLengthFixed);
			ContentPanel.Children.Add(checkBoxInvertVectorArrows);

			appliedSettings = settings;
			originalSettings = new VisualizerSettings();
			currentSettings = new VisualizerSettings();
			copyProperties(appliedSettings, originalSettings);
			copyProperties(appliedSettings, currentSettings);
			updateView();

			checkBoxInvertVectorArrows.IsCheckedChanged += (s, e) => currentSettings.InvertVectorArrows = checkBoxInvertVectorArrows.IsChecked == true;
			checkBoxIsArrowLengthFixed.IsCheckedChanged += (s, e) => currentSettings.IsArrowLengthFixed = checkBoxIsArrowLengthFixed.IsChecked == true;
			trackBarVectorLengthFactor.ValueChanged += (s, e) =>
			{
				if (isUpdatingArrowLengthView)
					return;
				currentSettings.ArrowLengthFactor = (int)trackBarVectorLengthFactor.Value * 0.01m;
				updateArrowLengthFactorTextbox();
			};
			textBoxVectorLengthFactor.TextChanged += (s, e) =>
			{
				if (isUpdatingArrowLengthView)
					return;
				if (decimal.TryParse(textBoxVectorLengthFactor.Text, out decimal value))
				{
					currentSettings.ArrowLengthFactor = value;
					updateTrackbarVectorLengthFactorValue();
				}
			};
		}

		internal IVisualizerSettings CurrentSettings => currentSettings;

		protected override void OnOK()
		{
			copyProperties(currentSettings, appliedSettings);
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		protected override void OnApply()
		{
			copyProperties(currentSettings, appliedSettings);
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		protected override void OnCancel()
		{
			copyProperties(originalSettings, appliedSettings);
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void updateView()
		{
			checkBoxInvertVectorArrows.IsChecked = currentSettings.InvertVectorArrows;
			checkBoxIsArrowLengthFixed.IsChecked = currentSettings.IsArrowLengthFixed;
			updateArrowLengthFactorTextbox();
			updateTrackbarVectorLengthFactorValue();
		}

		private void updateTrackbarVectorLengthFactorValue()
		{
			try
			{
				isUpdatingArrowLengthView = true;
				int valueForTrackbar = (int)(currentSettings.ArrowLengthFactor * 100m);
				valueForTrackbar = Math.Max(valueForTrackbar, (int)trackBarVectorLengthFactor.Minimum);
				valueForTrackbar = Math.Min(valueForTrackbar, (int)trackBarVectorLengthFactor.Maximum);
				trackBarVectorLengthFactor.Value = valueForTrackbar;
			}
			finally
			{
				isUpdatingArrowLengthView = false;
			}
		}

		private void updateArrowLengthFactorTextbox()
		{
			try
			{
				isUpdatingArrowLengthView = true;
				textBoxVectorLengthFactor.Text = currentSettings.ArrowLengthFactor.ToString();
			}
			finally
			{
				isUpdatingArrowLengthView = false;
			}
		}

		private static void copyProperties(IVisualizerSettings source, IVisualizerSettings destination)
		{
			destination.ArrowLengthFactor = source.ArrowLengthFactor;
			destination.InvertVectorArrows = source.InvertVectorArrows;
			destination.IsArrowLengthFixed = source.IsArrowLengthFixed;
		}
	}
}
