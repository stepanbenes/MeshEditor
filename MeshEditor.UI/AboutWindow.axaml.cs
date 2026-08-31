using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeshEditor.UI;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var title = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? "MeshEditor";
        var description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description
            ?? "3D graphical editor of finite element meshes.";
        var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        ProductNameText.Text = title;
        VersionText.Text = $"Version {fileVersion}";
        DescriptionText.Text = description;
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
