using System;
using System.IO;
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

        if (orbitButton is not null)
            orbitButton.Click += (_, _) => SetViewportTool(OpenGlSurface.ViewportTool.Orbit);
        if (panButton is not null)
            panButton.Click += (_, _) => SetViewportTool(OpenGlSurface.ViewportTool.Pan);
        if (zoomButton is not null)
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

    private async void SaveMesh_Click(object? sender, RoutedEventArgs e)
    {
        var suggestedName = !string.IsNullOrWhiteSpace(lastMeshPath)
            ? Path.GetFileNameWithoutExtension(lastMeshPath)
            : "mesh";

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save mesh file",
            SuggestedFileName = string.IsNullOrWhiteSpace(suggestedName) ? "mesh" : suggestedName,
            DefaultExtension = "msh",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Supported mesh formats")
                {
                    Patterns = new[] { "*.msh", "*.vtu", "*.obj", "*.ply", "*.mesh.json" }
                }
            }
        });

        if (file is null)
            return;

        var savePath = file.TryGetLocalPath() ?? file.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(savePath))
            return;

        viewportSurface?.SaveMesh(savePath);
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
