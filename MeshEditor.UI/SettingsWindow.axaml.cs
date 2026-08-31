using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MeshEditor.CoreInterface;
using MeshEditor.Graphics;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.UI;

public partial class SettingsWindow : Window
{
    private readonly OpenGlSurface viewportSurface;

    public SettingsWindow(OpenGlSurface viewportSurface)
    {
        InitializeComponent();
        this.viewportSurface = viewportSurface;
        SceneSettings.SaveState();
        populateValuesFromSettings();
    }

    private void populateValuesFromSettings()
    {
        var settings = SceneSettings.Instance;

        LineSmoothCheckBox.IsChecked = settings.LineSmooth;
        PointSmoothCheckBox.IsChecked = settings.PointSmooth;
        FaceLightingCheckBox.IsChecked = settings.FaceLighting;
        EdgeLightingCheckBox.IsChecked = settings.EdgeLighting;

        PointSizeUpDown.Value = (decimal)settings.PointSize;
        OrdinaryEdgeWidthUpDown.Value = (decimal)settings.OrdinaryEdgeWidth;
        BorderEdgeWidthUpDown.Value = (decimal)settings.BorderEdgeWidth;
        BeamWidthUpDown.Value = (decimal)settings.BeamWidth;

        ShadingModelComboBox.ItemsSource = Enum.GetValues<ShadingModel>();
        RenderModeComboBox.ItemsSource = Enum.GetValues<RenderMode>();
        ShadingModelComboBox.SelectedItem = settings.ShadingModel;
        RenderModeComboBox.SelectedItem = settings.DefaultRenderMode;
        SifelExtensionTextBox.Text = settings.SifelFileformatExtension;
    }

    private void applyValuesToSettings()
    {
        var settings = SceneSettings.Instance;

        settings.LineSmooth = LineSmoothCheckBox.IsChecked == true;
        settings.PointSmooth = PointSmoothCheckBox.IsChecked == true;
        settings.FaceLighting = FaceLightingCheckBox.IsChecked == true;
        settings.EdgeLighting = EdgeLightingCheckBox.IsChecked == true;

        settings.PointSize = (float)(PointSizeUpDown.Value ?? (decimal)settings.PointSize);
        settings.OrdinaryEdgeWidth = (float)(OrdinaryEdgeWidthUpDown.Value ?? (decimal)settings.OrdinaryEdgeWidth);
        settings.BorderEdgeWidth = (float)(BorderEdgeWidthUpDown.Value ?? (decimal)settings.BorderEdgeWidth);
        settings.BeamWidth = (float)(BeamWidthUpDown.Value ?? (decimal)settings.BeamWidth);

        if (ShadingModelComboBox.SelectedItem is ShadingModel shadingModel)
            settings.ShadingModel = shadingModel;
        if (RenderModeComboBox.SelectedItem is RenderMode renderMode)
            settings.DefaultRenderMode = renderMode;

        settings.SifelFileformatExtension = SifelExtensionTextBox.Text ?? settings.SifelFileformatExtension;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        applyValuesToSettings();
        viewportSurface.ApplySceneSettings();
        SceneSettings.SaveToConfigurationFile();
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        SceneSettings.RestoreState();
        viewportSurface.ApplySceneSettings();
        Close(false);
    }

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        SceneSettings.Reset();
        populateValuesFromSettings();
        viewportSurface.ApplySceneSettings();
    }
}
