using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeshEditor.UI;

public partial class ScreenshotOptionsWindow : Window
{
    private static bool savedUseSelectionArea;

    public ScreenshotOptionsWindow()
    {
        InitializeComponent();
        UseSelectionArea = savedUseSelectionArea;
        UpdateOkButtonText();
    }

    public bool UseSelectionArea
    {
        get => SelectionAreaRadioButton.IsChecked == true;
        set
        {
            SelectionAreaRadioButton.IsChecked = value;
            WholeSceneRadioButton.IsChecked = !value;
        }
    }

    private void Radio_Checked(object? sender, RoutedEventArgs e)
    {
        UpdateOkButtonText();
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        savedUseSelectionArea = UseSelectionArea;
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void UpdateOkButtonText()
    {
        OkButton.Content = UseSelectionArea ? "Select" : "Save";
    }
}
