namespace SqlPerformanceTester.Services.Interfaces;

public interface IDialogService
{
    void ShowError(string message, string title);
    void ShowInfo(string message, string title);
    void ShowWarning(string message, string title);
    string? ShowSaveFileDialog(string filter, string defaultExt, string fileName);
}
