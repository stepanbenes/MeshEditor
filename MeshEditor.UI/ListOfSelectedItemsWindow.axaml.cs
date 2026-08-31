using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MeshEditor.Data;

namespace MeshEditor.UI;

public partial class ListOfSelectedItemsWindow : Window
{
    private readonly OpenGlSurface viewportSurface;
    private bool refreshing;

    public ListOfSelectedItemsWindow(OpenGlSurface viewportSurface)
    {
        InitializeComponent();
        this.viewportSurface = viewportSurface;
        EntityTypeComboBox.SelectionChanged += async (_, _) => await RefreshAsync();
        ShowCompleteInfoCheckBox.Click += async (_, _) => await RefreshAsync();
        updateNodePropertyButtons();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (refreshing)
            return;

        refreshing = true;
        try
        {
            updateNodePropertyButtons();
            var itemType = getSelectedItemType();
            var showCompleteInfo = ShowCompleteInfoCheckBox.IsChecked == true;
            var snapshot = await viewportSurface.GetSelectedItemsDescriptionAsync(itemType, showCompleteInfo);
            if (snapshot is null || !snapshot.HasMesh)
            {
                CountTextBlock.Text = string.Empty;
                ItemsTextBox.Text = "No mesh loaded";
                return;
            }

            CountTextBlock.Text = $"({snapshot.Count} {toLabel(itemType, snapshot.Count)})";
            ItemsTextBox.Text = snapshot.Description;
        }
        catch (Exception ex)
        {
            ItemsTextBox.Text = $"Error: {ex.Message}";
        }
        finally
        {
            refreshing = false;
        }
    }

    private async void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void AddProperty_Click(object? sender, RoutedEventArgs e)
    {
        var value = await promptPropertyAsync("Specify property to be added");
        if (value is null)
            return;

        viewportSurface.AddPropertyToSelectedNodes(value.Value);
        await RefreshAsync();
    }

    private async void RemoveProperty_Click(object? sender, RoutedEventArgs e)
    {
        var value = await promptPropertyAsync("Specify property to be removed");
        if (value is null)
            return;

        viewportSurface.RemovePropertyFromSelectedNodes(value.Value);
        await RefreshAsync();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private ItemTypeToSelect getSelectedItemType()
    {
        return EntityTypeComboBox.SelectedIndex switch
        {
            0 => ItemTypeToSelect.Node,
            1 => ItemTypeToSelect.Element,
            2 => ItemTypeToSelect.Face,
            3 => ItemTypeToSelect.Edge,
            _ => ItemTypeToSelect.Node
        };
    }

    private void updateNodePropertyButtons()
    {
        var isNodeSelection = getSelectedItemType() == ItemTypeToSelect.Node;
        AddPropertyButton.IsVisible = isNodeSelection;
        RemovePropertyButton.IsVisible = isNodeSelection;
    }

    private async Task<int?> promptPropertyAsync(string description)
    {
        var parsedValue = 0;
        var dialog = new ValuePromptDialog("Insert property number", description, "0");
        dialog.Validator = d => int.TryParse(d.InputValue, out parsedValue)
            ? (true, null)
            : (false, $"Please input valid integer value in range <{int.MinValue}; {int.MaxValue}>.");

        if (!await dialog.ShowDialog<bool>(this))
            return null;

        return parsedValue;
    }

    private static string toLabel(ItemTypeToSelect itemType, int count)
    {
        return itemType switch
        {
            ItemTypeToSelect.Node => count == 1 ? "node" : "nodes",
            ItemTypeToSelect.Element => count == 1 ? "element" : "elements",
            ItemTypeToSelect.Face => count == 1 ? "face" : "faces",
            ItemTypeToSelect.Edge => count == 1 ? "edge" : "edges",
            _ => "items"
        };
    }
}
