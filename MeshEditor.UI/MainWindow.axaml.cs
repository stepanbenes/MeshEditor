using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MeshEditor.CoreInterface;

namespace MeshEditor.UI;

public partial class MainWindow : Window
{
    private static string savedInputValue = "0";
    private static string savedCheckedInputValue = "0";
    private static string? takeScreenshotLastFilename;

    private OpenGlSurface? viewportSurface;
    private Button? saveButton;
    private TextBlock? statusBarText;
    private TextBlock? scenePathText;
    private string? lastMeshPath;

    public MainWindow()
    {
        InitializeComponent();
        viewportSurface = this.FindControl<OpenGlSurface>("ViewportSurface");
        saveButton = this.FindControl<Button>("SaveButton");
        statusBarText = this.FindControl<TextBlock>("StatusBarText");
        scenePathText = this.FindControl<TextBlock>("ScenePathText");
        if (viewportSurface is not null)
            viewportSurface.StatusChanged += HandleViewportStatusChanged;

        var orbitButton = this.FindControl<Button>("OrbitButton");
        var panButton = this.FindControl<Button>("PanButton");
        var zoomButton = this.FindControl<Button>("ZoomButton");

        if (orbitButton is not null)
            orbitButton.Click += (_, _) => SetViewportTool(OpenGlSurface.ViewportTool.Orbit);
        if (panButton is not null)
            panButton.Click += (_, _) => SetViewportTool(OpenGlSurface.ViewportTool.Pan);
        if (zoomButton is not null)
            zoomButton.Click += (_, _) => SetViewportTool(OpenGlSurface.ViewportTool.Zoom);

        updateSceneInfo();
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
        updateSceneInfo();
        setStatus("Loading mesh...");
    }

    private void RefreshViewport_Click(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(lastMeshPath))
            viewportSurface?.LoadMesh(lastMeshPath);
    }

    private async void SaveMesh_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(lastMeshPath))
        {
            setStatus("No mesh loaded");
            return;
        }

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
        setStatus("Saving mesh...");
    }

    private async void TakeScreenshot_Click(object? sender, RoutedEventArgs e)
    {
        if (viewportSurface is null)
            return;

        var optionsDialog = new ScreenshotOptionsWindow();
        if (!await optionsDialog.ShowDialog<bool>(this))
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save screenshot",
            SuggestedFileName = string.IsNullOrWhiteSpace(takeScreenshotLastFilename) ? "screenshot" : takeScreenshotLastFilename,
            DefaultExtension = "png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG image") { Patterns = new[] { "*.png" } },
                new FilePickerFileType("JPEG image") { Patterns = new[] { "*.jpg", "*.jpeg" } },
                new FilePickerFileType("Bitmap image") { Patterns = new[] { "*.bmp" } }
            }
        });

        if (file is null)
            return;

        var savePath = file.TryGetLocalPath() ?? file.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(savePath))
            return;

        takeScreenshotLastFilename = Path.GetFileNameWithoutExtension(savePath);
        if (optionsDialog.UseSelectionArea)
        {
            setStatus("Selection-area screenshot not yet implemented; saving whole scene");
        }

        if (viewportSurface.SaveScreenshot(savePath))
            setStatus($"Screenshot saved: {savePath}");
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

    protected override void OnClosed(EventArgs e)
    {
        if (viewportSurface is not null)
            viewportSurface.StatusChanged -= HandleViewportStatusChanged;

        base.OnClosed(e);
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new AboutWindow();
        await dialog.ShowDialog(this);
    }

    private async void SignalNode_Click(object? sender, RoutedEventArgs e)
    {
        if (viewportSurface is null)
            return;

        var parsedIds = new List<int>();
        var dialog = new ValuePromptDialog("Insert node ID", "Signal node(s) with ID(s):", savedInputValue);
        dialog.Validator = d =>
        {
            parsedIds.Clear();
            var parts = d.InputValue.Split([',', ';', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return (false, "Please input at least one integer value.");

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var value))
                    return (false, "Please input valid integer value(s).");
                parsedIds.Add(value);
            }

            return (true, null);
        };

        if (await dialog.ShowDialog<bool>(this))
        {
            savedInputValue = dialog.InputValue;
            viewportSurface.SignalNodes(parsedIds.ToArray());
            setStatus($"Signalled {parsedIds.Count} node(s)");
        }
    }

    private async void SignalElement_Click(object? sender, RoutedEventArgs e)
    {
        if (viewportSurface is null)
            return;

        var elementId = 0;
        var dialog = new ValuePromptDialog("Insert element ID", "Signal element with ID:", savedInputValue);
        dialog.ConfigureOption("Clear signalled element", isChecked: false);
        dialog.Validator = d =>
        {
            if (d.IsOptionChecked)
                return (true, null);

            return int.TryParse(d.InputValue, out elementId)
                ? (true, null)
                : (false, "Please input valid integer value.");
        };

        if (await dialog.ShowDialog<bool>(this))
        {
            savedInputValue = dialog.InputValue;
            if (dialog.IsOptionChecked)
            {
                viewportSurface.ClearSignalElement();
                setStatus("Cleared signalled element");
            }
            else
            {
                viewportSurface.SignalElement(elementId);
                setStatus($"Signalled element {elementId}");
            }
        }
    }

    private async void MeshInfo_Click(object? sender, RoutedEventArgs e)
    {
        if (viewportSurface is null)
            return;

        var dialog = new MeshInfoWindow(viewportSurface);
        await dialog.ShowDialog(this);
    }

    private async void ListOfSelectedItems_Click(object? sender, RoutedEventArgs e)
    {
        if (viewportSurface is null)
            return;

        var dialog = new ListOfSelectedItemsWindow(viewportSurface);
        await dialog.ShowDialog(this);
    }

    private async void Options_Click(object? sender, RoutedEventArgs e)
    {
        if (viewportSurface is null)
            return;

        var dialog = new SettingsWindow(viewportSurface);
        if (await dialog.ShowDialog<bool>(this))
            setStatus("Settings updated");
    }

    private async void SetPropertyOfSelectedItems_Click(object? sender, RoutedEventArgs e)
    {
        if (viewportSurface is null)
            return;

        var propertyValue = 0;
        var snapshot = await viewportSurface.GetMeshInfoAsync();
        var description = snapshot?.SelectedItemsDescription;
        if (string.IsNullOrWhiteSpace(description))
            description = "Nothing selected";

        var dialog = new ValuePromptDialog("Insert property number", description, savedInputValue);
        dialog.Validator = d => int.TryParse(d.InputValue, out propertyValue)
            ? (true, null)
            : (false, $"Please input valid integer value in range <{int.MinValue}; {int.MaxValue}>.");

        if (await dialog.ShowDialog<bool>(this))
        {
            savedInputValue = dialog.InputValue;
            viewportSurface.SetPropertyOfSelectedItems(propertyValue);
            setStatus($"Applied property {propertyValue}");
        }
    }

    private async void SelectItemsByProperty_Click(object? sender, RoutedEventArgs e)
    {
        await selectItemsByPropertyInternal(addToSelection: false);
    }

    private async void SelectItemsByPropertyAdd_Click(object? sender, RoutedEventArgs e)
    {
        await selectItemsByPropertyInternal(addToSelection: true);
    }

    private void SelectAllItems_Click(object? sender, RoutedEventArgs e)
    {
        viewportSurface?.PerformSimpleAction(AvailableAction.SelectAllItems);
        setStatus("Selected all items");
    }

    private void UnselectAllItems_Click(object? sender, RoutedEventArgs e)
    {
        viewportSurface?.PerformSimpleAction(AvailableAction.UnselectAllItems);
        setStatus("Selection cleared");
    }

    private void InvertSelection_Click(object? sender, RoutedEventArgs e)
    {
        viewportSurface?.PerformSimpleAction(AvailableAction.InvertSelection);
        setStatus("Selection inverted");
    }

    private void SelectIncidingItems_Click(object? sender, RoutedEventArgs e)
    {
        viewportSurface?.PerformSimpleAction(AvailableAction.SelectIncidingItems);
        setStatus("Selected inciding items");
    }

    private void DeleteSelectedElements_Click(object? sender, RoutedEventArgs e)
    {
        viewportSurface?.PerformSimpleAction(AvailableAction.DeleteSelectedElements);
        setStatus("Deleting selected elements...");
    }

    private void RestoreMesh_Click(object? sender, RoutedEventArgs e)
    {
        viewportSurface?.PerformSimpleAction(AvailableAction.RestoreMesh);
        setStatus("Restoring mesh...");
    }

    private void ClearSignalNode_Click(object? sender, RoutedEventArgs e)
    {
        viewportSurface?.ClearSignalNode();
        setStatus("Cleared signalled node");
    }

    private void ClearSignalElement_Click(object? sender, RoutedEventArgs e)
    {
        viewportSurface?.ClearSignalElement();
        setStatus("Cleared signalled element");
    }

    private async System.Threading.Tasks.Task selectItemsByPropertyInternal(bool addToSelection)
    {
        if (viewportSurface is null)
            return;

        var propertyValue = 0;
        var dialog = new ValuePromptDialog(
            "Insert property number",
            "Select items by property:",
            savedCheckedInputValue);
        dialog.ConfigureOption("add to selection", addToSelection);
        dialog.Validator = d =>
        {
            if (!int.TryParse(d.InputValue, out propertyValue) || propertyValue < 0)
                return (false, $"Please input valid integer value in range <0; {int.MaxValue}>.");

            return (true, null);
        };

        if (await dialog.ShowDialog<bool>(this))
        {
            savedCheckedInputValue = dialog.InputValue;
            viewportSurface.SelectItemsByProperty(propertyValue, dialog.IsOptionChecked);
            setStatus($"Selected items with property {propertyValue}");
        }
    }

    private void HandleViewportStatusChanged(string status)
    {
        setStatus(status);
    }

    private void updateSceneInfo()
    {
        if (scenePathText is not null)
            scenePathText.Text = !string.IsNullOrWhiteSpace(lastMeshPath) ? lastMeshPath : "No mesh loaded";

        if (saveButton is not null)
            saveButton.IsEnabled = !string.IsNullOrWhiteSpace(lastMeshPath);
    }

    private void setStatus(string status)
    {
        if (statusBarText is not null && !string.IsNullOrWhiteSpace(status))
            statusBarText.Text = status;
    }
}
