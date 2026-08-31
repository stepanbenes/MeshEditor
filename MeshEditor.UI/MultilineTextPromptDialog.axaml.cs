using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeshEditor.UI;

public partial class MultilineTextPromptDialog : Window
{
    public string InputText => InputTextBox.Text ?? string.Empty;

    public MultilineTextPromptDialog(string title, string description, string initialValue, bool singleLine = false)
    {
        InitializeComponent();
        Title = title;
        PromptTextBlock.Text = title;
        DescriptionTextBlock.Text = description;
        InputTextBox.Text = initialValue ?? string.Empty;
        if (singleLine)
        {
            Height = 240;
            MinHeight = 200;
            InputTextBox.AcceptsReturn = false;
            InputTextBox.TextWrapping = Avalonia.Media.TextWrapping.NoWrap;
        }
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
