using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace MeshEditor.UI;

public partial class ValuePromptDialog : Window
{
    public Func<ValuePromptDialog, (bool IsValid, string? ErrorMessage)>? Validator { get; set; }

    public ValuePromptDialog(string title, string prompt, string initialValue = "")
    {
        InitializeComponent();
        Title = title;
        PromptTextBlock.Text = prompt;
        InputTextBox.Text = initialValue;
        InputTextBox.Focus();
        InputTextBox.SelectAll();
        KeyDown += OnKeyDown;
    }

    public string InputValue => InputTextBox.Text ?? string.Empty;
    public bool IsOptionChecked => OptionCheckBox.IsChecked == true;

    public void ConfigureOption(string text, bool isChecked = false)
    {
        OptionCheckBox.Content = text;
        OptionCheckBox.IsChecked = isChecked;
        OptionCheckBox.IsVisible = true;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        ValidateAndClose();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(false);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ValidateAndClose();
            e.Handled = true;
        }
    }

    private void ValidateAndClose()
    {
        var validation = Validator?.Invoke(this) ?? (true, null);
        if (validation.IsValid)
        {
            ValidationTextBlock.IsVisible = false;
            Close(true);
            return;
        }

        ValidationTextBlock.Text = string.IsNullOrWhiteSpace(validation.ErrorMessage) ? "Invalid value." : validation.ErrorMessage;
        ValidationTextBlock.IsVisible = true;
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }
}
