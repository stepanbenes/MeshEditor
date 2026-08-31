using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeshEditor.UI;

public partial class MeshInfoWindow : Window
{
    private readonly OpenGlSurface viewportSurface;

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
            PropertyRowsList.ItemsSource = new List<string>();
            return;
        }

        MeshStateText.Text = snapshot.MeshHasHiddenElements ? "Mesh loaded (after cut)" : "Mesh loaded";
        NodeCountText.Text = $"Node count: {snapshot.NodeCount}";
        ElementCountText.Text = $"Element count: {snapshot.ElementCount}";
        FaceEdgeCountText.Text = $"Face count: {snapshot.FaceCount}, Edge count: {snapshot.EdgeCount}";
        BeamCountText.Text = $"Beam count: {snapshot.BeamCount}";
        SelectionText.Text = $"Selection: {snapshot.SelectedItemsDescription}";
        PropertyRowsList.ItemsSource = snapshot.PropertyRows;
    }

    private async void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
