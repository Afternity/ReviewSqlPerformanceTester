using System.Windows;
using Microsoft.Win32;
using SqlPerformanceTester.Services.Interfaces;

namespace SqlPerformanceTester.Services;

public class DialogService : IDialogService
{
    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowInfo(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public string? ShowSaveFileDialog(string filter, string defaultExt, string fileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt,
            FileName = fileName
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
