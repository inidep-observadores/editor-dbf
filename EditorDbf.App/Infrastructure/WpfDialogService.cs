using System;
using System.Windows;
using Microsoft.Win32;
using EditorDbf.App.Views;

namespace EditorDbf.App.Infrastructure;

public sealed class WpfDialogService : IDialogService
{
    public MessageBoxResult ShowConfirm(string message, string title, MessageBoxButton buttons = MessageBoxButton.YesNo)
    {
        return MessageBox.Show(message, title, buttons, MessageBoxImage.Question);
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowInfo(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public string? ShowInput(string prompt, string title, string defaultValue = "")
    {
        return InputDialog.Show(prompt, title, defaultValue);
    }

    public string[]? ShowOpenFileDialog(string title, string filter, bool multiselect = false)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = multiselect
        };

        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            return dialog.FileNames;
        }

        return null;
    }
}
