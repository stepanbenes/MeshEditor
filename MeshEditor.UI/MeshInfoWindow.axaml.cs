using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MeshEditor.Data;

namespace MeshEditor.UI;

public partial class MeshInfoWindow : Window
{
    private sealed class PropertyRowView
    {
        public int Property { get; init; }
        public EntityType EntityType { get; init; }
        public string Commands { get; init; } = string.Empty;
        public string Comment { get; init; } = string.Empty;
    }

    private readonly OpenGlSurface viewportSurface;
    private List<PropertyRowView> currentRows = new();

    public MeshInfoWindow(OpenGlSurface viewportSurface)
    {
        InitializeComponent();
        this.viewportSurface = viewportSurface;
        _ = RefreshAsync();
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        var snapshot = await viewportSurface.GetMeshInfoAsync();
        if (snapshot is null || !snapshot.HasMesh)
        {
            MeshStateText.Text = "No mesh loaded";
            NodeCountText.Text = string.Empty;
            ElementCountText.Text = string.Empty;
            FaceEdgeCountText.Text = string.Empty;
            BeamCountText.Text = string.Empty;
            SelectionText.Text = string.Empty;
            currentRows = new List<PropertyRowView>();
            PropertyRowsListBox.ItemsSource = Array.Empty<string>();
            StatusText.Text = string.Empty;
            return;
        }

        MeshStateText.Text = snapshot.MeshHasHiddenElements ? "Mesh loaded (after cut)" : "Mesh loaded";
        NodeCountText.Text = $"Node count: {snapshot.NodeCount}";
        ElementCountText.Text = $"Element count: {snapshot.ElementCount}";
        FaceEdgeCountText.Text = $"Face count: {snapshot.FaceCount}, Edge count: {snapshot.EdgeCount}";
        BeamCountText.Text = $"Beam count: {snapshot.BeamCount}";
        SelectionText.Text = $"Selection: {snapshot.SelectedItemsDescription}";
        currentRows = snapshot.PropertyRows
            .Select(r => new PropertyRowView
            {
                Property = r.Property,
                EntityType = r.EntityType,
                Commands = r.Commands,
                Comment = r.Comment
            })
            .OrderBy(r => r.Property)
            .ThenBy(r => r.EntityType.ToString())
            .ToList();
        PropertyRowsListBox.ItemsSource = currentRows.Select(formatRowText).ToArray();
        StatusText.Text = string.Empty;
    }

    private async void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void EditCommands_Click(object? sender, RoutedEventArgs e)
    {
        var row = getSelectedRow();
        if (row is null)
        {
            StatusText.Text = "Select a property row first.";
            return;
        }

        var dialog = new MultilineTextPromptDialog(
            $"Edit commands for property {row.Property} [{row.EntityType}]",
            "One command per line",
            row.Commands);
        if (!await dialog.ShowDialog<bool>(this))
            return;

        var lines = (dialog.InputText ?? string.Empty)
            .Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        var result = await viewportSurface.SetPropertyCommandsAsync(row.Property, row.EntityType, lines);
        if (!result.success)
        {
            StatusText.Text = result.error ?? "Failed to update commands.";
            return;
        }

        await RefreshAsync();
    }

    private async void EditComment_Click(object? sender, RoutedEventArgs e)
    {
        var row = getSelectedRow();
        if (row is null)
        {
            StatusText.Text = "Select a property row first.";
            return;
        }

        var dialog = new MultilineTextPromptDialog(
            $"Set description of property {row.Property}",
            $"Property {row.Property}:",
            row.Comment,
            singleLine: true);
        if (!await dialog.ShowDialog<bool>(this))
            return;

        var success = await viewportSurface.SetPropertyCommentAsync(row.Property, dialog.InputText);
        if (!success)
        {
            StatusText.Text = "Failed to update property comment.";
            return;
        }

        await RefreshAsync();
    }

    private PropertyRowView? getSelectedRow()
    {
        var selectedIndex = PropertyRowsListBox.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= currentRows.Count)
            return null;
        return currentRows[selectedIndex];
    }

    private static string formatRowText(PropertyRowView row)
    {
        var commands = string.IsNullOrWhiteSpace(row.Commands) ? "(none)" : row.Commands.Replace("\r", " ").Replace("\n", " | ");
        var comment = string.IsNullOrWhiteSpace(row.Comment) ? string.Empty : $"  Comment: {row.Comment}";
        return $"{row.Property} [{row.EntityType}]  Commands: {commands}{comment}";
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
