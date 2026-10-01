using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// Parameters of a filter creating a new layer (same as in MeshEditor.DataVisualizer.UI.FilterParamsForm.cs).
	/// </summary>
	public class FilterParams
	{
		public FilterParams(IReadOnlyCollection<string> filterParameters, IReadOnlyCollection<decimal> keyTimeSteps, IReadOnlyCollection<string> compressionParameters, string layerName, string constraintFieldName)
		{
			FilterParameters = filterParameters;
			KeyTimeSteps = keyTimeSteps;
			CompressionParameters = compressionParameters;
			LayerName = layerName;
			ConstraintFieldName = constraintFieldName;
		}

		public IReadOnlyCollection<string> FilterParameters { get; }
		public IReadOnlyCollection<decimal> KeyTimeSteps { get; }
		public IReadOnlyCollection<string> CompressionParameters { get; }
		public string LayerName { get; }
		public string ConstraintFieldName { get; }
	}

	/// <summary>
	/// Base of the filter parameter dialogs (port of FilterParamsForm and FormExtensions.ShowAsync).
	/// </summary>
	public abstract class FilterParamsDialog : Window
	{
		private FilterParams filterParams;
		private bool dialogResultOK;
		protected readonly StackPanel ContentPanel = new StackPanel { Spacing = 4 };
		internal readonly Button buttonOK = new Button { Content = "OK", MinWidth = 75, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = true };
		internal readonly Button buttonCancel = new Button { Content = "Cancel", MinWidth = 75, HorizontalContentAlignment = HorizontalAlignment.Center, IsCancel = true };

		protected FilterParamsDialog(string title)
		{
			Title = title;
			CanMinimize = CanMaximize = false;
			ShowInTaskbar = false;
			SizeToContent = SizeToContent.WidthAndHeight;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			buttonOK.Click += (s, e) =>
			{
				FilterParams = CreateFilterParams();
				dialogResultOK = true;
				Close();
			};
			buttonCancel.Click += (s, e) => Close();

			var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Margin = new Thickness(0, 10, 0, 0), Children = { buttonOK, buttonCancel } };
			var root = new DockPanel { Margin = new Thickness(12), MinWidth = 340 };
			DockPanel.SetDock(buttons, Dock.Bottom);
			root.Children.Add(buttons);
			root.Children.Add(ContentPanel);
			Content = root;
		}

		public FilterParams FilterParams
		{
			get => filterParams ?? throw new InvalidOperationException("Filter params should be requested only if dialog result is OK");
			protected set => filterParams = value ?? throw new ArgumentNullException();
		}

		protected abstract FilterParams CreateFilterParams();

		protected static TextBlock Label(string text) => new TextBlock { Text = text, Margin = new Thickness(0, 6, 0, 0) };

		/// <summary>
		/// Shows the dialog modally or not (FormExtensions.ShowAsync); true when confirmed by OK.
		/// </summary>
		public async Task<bool> ShowAsync(Window owner, bool modal)
		{
			owner ??= DialogOwner.Current;
			if (modal && owner != null)
			{
				await ShowDialog(owner);
			}
			else
			{
				var closed = new TaskCompletionSource();
				Closed += (s, e) => closed.TrySetResult();
				if (owner != null)
					Show(owner);
				else
					Show();
				await closed.Task;
			}
			return dialogResultOK;
		}
	}

	/// <summary>
	/// Port of DeformationFilterParamsForm.
	/// </summary>
	public class DeformationFilterParamsDialog : FilterParamsDialog
	{
		internal readonly ComboBox comboBoxDeformationField = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		internal readonly TextBox textBoxLayerName = new TextBox { Text = "deformation" };
		internal readonly Slider trackBarScale = new Slider { Minimum = 0, Maximum = 100, Value = 10, TickFrequency = 1, IsSnapToTickEnabled = true };
		internal readonly TextBlock labelScale = new TextBlock { Text = "Scale" };

		public DeformationFilterParamsDialog(IEnumerable<string> availableVectorFields)
			: base("Deformation filter parameters")
		{
			ContentPanel.Children.Add(Label("Field with displacements information"));
			ContentPanel.Children.Add(comboBoxDeformationField);
			ContentPanel.Children.Add(labelScale);
			ContentPanel.Children.Add(trackBarScale);
			ContentPanel.Children.Add(Label("Deformation layer name"));
			ContentPanel.Children.Add(textBoxLayerName);

			var availableVectorFieldsArray = availableVectorFields.ToArray();
			comboBoxDeformationField.ItemsSource = availableVectorFieldsArray;
			comboBoxDeformationField.SelectedItem = availableVectorFieldsArray.FirstOrDefault(field => field.StartsWith("displacement", StringComparison.InvariantCultureIgnoreCase));
			trackBarScale.ValueChanged += (s, e) => trackBarScale_ValueChanged();
			trackBarScale_ValueChanged();
		}

		private double getScaleValue() => (int)trackBarScale.Value * 0.01;

		private void trackBarScale_ValueChanged()
		{
			double scale = getScaleValue();
			labelScale.Text = $"Scale: {scale:P0}";
			textBoxLayerName.Text = $"deformation (scale: {scale.ToString(CultureInfo.InvariantCulture)})";
		}

		protected override FilterParams CreateFilterParams()
		{
			string layerNameText = textBoxLayerName.Text;
			return new FilterParams(
				filterParameters: new[] { comboBoxDeformationField.SelectedItem as string, getScaleValue().ToString(CultureInfo.InvariantCulture) },
				keyTimeSteps: new decimal[0], // ignored for deformation filter
				compressionParameters: new string[0], // ignored for deformation filter
				layerName: string.IsNullOrWhiteSpace(layerNameText) ? null : layerNameText,
				constraintFieldName: null);
		}
	}

	/// <summary>
	/// Port of SliceFilterParamsForm (non-modal; the plane is defined by points picked on the mesh).
	/// </summary>
	public class SliceFilterParamsDialog : FilterParamsDialog
	{
		private readonly SceneFacade scene;
		private int requiredNumberOfPoints;
		internal readonly Button buttonSelectTwoPoints = new Button { Content = "Define plane using two points", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
		internal readonly Button buttonSelectThreePoints = new Button { Content = "Define plane using three points", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
		internal readonly TextBlock labelSuggestion = new TextBlock { Text = "Select plane definition points by clicking on nodes", IsVisible = false, Foreground = Avalonia.Media.Brushes.DarkBlue };
		internal readonly TextBox textBoxLayerName = new TextBox { Text = "slice" };

		public SliceFilterParamsDialog(SceneFacade scene)
			: base("Slice filter parameters")
		{
			ContentPanel.Children.Add(buttonSelectTwoPoints);
			ContentPanel.Children.Add(buttonSelectThreePoints);
			ContentPanel.Children.Add(labelSuggestion);
			ContentPanel.Children.Add(Label("Slice layer name"));
			ContentPanel.Children.Add(textBoxLayerName);

			this.scene = scene;
			this.scene.CutPlaneDefinitionPointsChanged += scene_CutPlaneDefinitionPointsChanged;
			buttonOK.IsEnabled = false; // enabled only after a cut plane is defined
			buttonSelectTwoPoints.Click += (s, e) => startPicking(2);
			buttonSelectThreePoints.Click += (s, e) => startPicking(3);
		}

		protected override void OnClosed(EventArgs e)
		{
			scene.CutPlaneDefinitionPointsChanged -= scene_CutPlaneDefinitionPointsChanged;
			SceneFacade.EditorMode = EditorMode.Orbit;
			clearCutPlanesAndPoints();
			base.OnClosed(e);
		}

		private void clearCutPlanesAndPoints()
		{
			scene.CutPlanes.Clear(); // hide cut planes
			scene.PerformAction(AvailableAction.ClearPlaneDefinitionPoints); // remove definition points and refresh scene
		}

		internal void startPicking(int numberOfPoints)
		{
			requiredNumberOfPoints = numberOfPoints;
			clearCutPlanesAndPoints();
			SceneFacade.EditorMode = EditorMode.PickCuttingPlanePoint;
			labelSuggestion.IsVisible = true;
		}

		private void scene_CutPlaneDefinitionPointsChanged(object sender, EventArgs e)
		{
			if (scene.CutPlaneDefinitionPointsCount >= requiredNumberOfPoints)
			{
				labelSuggestion.IsVisible = false;
				scene.PerformAction(AvailableAction.CreateCutPlane);
				if (scene.CutPlanes.Count > 0)
				{
					textBoxLayerName.Text = "slice " + scene.CutPlanes[0].Offset.ToString("0.00", CultureInfo.InvariantCulture);
					buttonOK.IsEnabled = true;
				}
			}
		}

		protected override FilterParams CreateFilterParams()
		{
			Debug.Assert(scene.CutPlanes.Count == 1);
			CutPlane cutPlane = scene.CutPlanes.Single();
			var normal = cutPlane.NormalVector;
			var offset = cutPlane.Offset;
			string layerNameText = textBoxLayerName.Text;
			return new FilterParams(
				filterParameters: new[] { normal.X, normal.Y, normal.Z, offset }.Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray(),
				keyTimeSteps: new decimal[0], // no key time steps for now
				compressionParameters: new string[0], // no compression for now
				layerName: string.IsNullOrWhiteSpace(layerNameText) ? null : layerNameText,
				constraintFieldName: null);
		}
	}

	/// <summary>
	/// Port of IsoSurfaceFilterParamsForm.
	/// </summary>
	public class IsoSurfaceFilterParamsDialog : FilterParamsDialog
	{
		private readonly IReadOnlyDictionary<string, IEnumerable<string>> availableFieldComponents;
		internal readonly ComboBox comboBoxField = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		internal readonly ComboBox comboBoxComponent = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		internal readonly TextBox textBoxValue = new TextBox();
		internal readonly TextBox textBoxLayerName = new TextBox();

		public IsoSurfaceFilterParamsDialog(IReadOnlyDictionary<string, IEnumerable<string>> availableFieldComponents)
			: base("Iso-surface filter parameters")
		{
			Debug.Assert(availableFieldComponents != null);
			ContentPanel.Children.Add(Label("Field"));
			ContentPanel.Children.Add(comboBoxField);
			ContentPanel.Children.Add(Label("Component"));
			ContentPanel.Children.Add(comboBoxComponent);
			ContentPanel.Children.Add(Label("Value"));
			ContentPanel.Children.Add(textBoxValue);
			ContentPanel.Children.Add(Label("Iso-surface layer name"));
			ContentPanel.Children.Add(textBoxLayerName);

			this.availableFieldComponents = availableFieldComponents;
			comboBoxField.SelectionChanged += (s, e) => { setupComponents(); updateLayerName(); };
			comboBoxComponent.SelectionChanged += (s, e) => updateLayerName();
			textBoxValue.TextChanged += (s, e) => updateLayerName();

			comboBoxField.ItemsSource = availableFieldComponents.Keys.ToArray();
			if (comboBoxField.ItemCount > 0)
				comboBoxField.SelectedIndex = 0;
			textBoxValue.Text = 0.0.ToString(CultureInfo.InvariantCulture);
			updateLayerName();
		}

		private void setupComponents()
		{
			comboBoxComponent.ItemsSource = comboBoxField.SelectedItem is string selectedField && availableFieldComponents.TryGetValue(selectedField, out var components)
				? components.ToArray()
				: Array.Empty<string>();
			if (comboBoxComponent.ItemCount > 0)
				comboBoxComponent.SelectedIndex = 0;
		}

		private void updateLayerName()
		{
			if (!double.TryParse(textBoxValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
				value = 0.0;
			textBoxLayerName.Text = $"isosurface {comboBoxField.SelectedItem as string}/{comboBoxComponent.SelectedItem as string} = {value.ToString(CultureInfo.InvariantCulture)}";
		}

		protected override FilterParams CreateFilterParams()
		{
			if (!double.TryParse(textBoxValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
				value = 0.0; // should not happen
			string layerNameText = textBoxLayerName.Text;
			return new FilterParams(
				filterParameters: new[] { comboBoxField.SelectedItem as string, comboBoxComponent.SelectedItem as string, value.ToString(CultureInfo.InvariantCulture) },
				keyTimeSteps: new decimal[0], // no key time steps for now
				compressionParameters: new string[0], // no compression for now
				layerName: string.IsNullOrWhiteSpace(layerNameText) ? null : layerNameText,
				constraintFieldName: null);
		}
	}
}
