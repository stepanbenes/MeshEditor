using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MeshEditor.CoreInterface;

namespace MeshEditor.UI;

public partial class MainWindow : Window
{
    private OpenGlSurface? viewportSurface;
    private string? lastMeshPath;

    public MainWindow()
    {
        InitializeComponent();
        viewportSurface = this.FindControl<OpenGlSurface>("ViewportSurface");

        var orbitButton = this.FindControl<Button>("OrbitButton");
        var panButton = this.FindControl<Button>("PanButton");
        var zoomButton = this.FindControl<Button>("ZoomButton");

        orbitButton.Click += (_, _) => SetViewportTool(OpenGlSurface.ViewportTool.Orbit);
        panButton.Click += (_, _) => SetViewportTool(OpenGlSurface.ViewportTool.Pan);
        zoomButton.Click += (_, _) => SetViewportTool(OpenGlSurface.ViewportTool.Zoom);
    }

    private async void OpenMesh_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open mesh file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Supported mesh formats")
                {
                    Patterns = new[] { "*.msh", "*.vtu", "*.obj", "*.ply", "*.mesh.json" }
                }
            }
        });

        if (files.Count == 0)
            return;

        lastMeshPath = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        viewportSurface?.LoadMesh(lastMeshPath);
    }

    private void RefreshViewport_Click(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(lastMeshPath))
            viewportSurface?.LoadMesh(lastMeshPath);
    }

    private void SetViewportTool(OpenGlSurface.ViewportTool tool)
    {
        viewportSurface?.SetTool(tool);
        if (tool == OpenGlSurface.ViewportTool.Orbit)
        {
            SceneFacade.EditorMode = EditorMode.Orbit;
        }
        else if (tool == OpenGlSurface.ViewportTool.Pan)
        {
            SceneFacade.EditorMode = EditorMode.Pan;
        }
        else if (tool == OpenGlSurface.ViewportTool.Zoom)
        {
            SceneFacade.EditorMode = EditorMode.ZoomWindow;
        }
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
