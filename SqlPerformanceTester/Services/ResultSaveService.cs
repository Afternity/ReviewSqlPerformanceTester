using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using SqlPerformanceTester.Common.Constants;
using SqlPerformanceTester.Models;
using SqlPerformanceTester.Services.Interfaces;

namespace SqlPerformanceTester.Services;

public class ResultSaveService : IResultSaveService
{
    private readonly IDialogService _dialogService;

    public ResultSaveService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public void SaveResults(
        IReadOnlyCollection<TestResult> results,
        string queryTemplate,
        string outputFile)
    {
        try
        {
            var hasPercentMarkers = queryTemplate.Contains("%");
            var filePath = Path.GetFullPath(outputFile);
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            // На случай если % нет
            //writer.WriteLine("ExecutionTimeMs,ID,ThreadId,Timestamp,IsError,ErrorMessage");

            if (hasPercentMarkers)
                writer.WriteLine(FileConstants.CsvHeaderWithId);
            else
                writer.WriteLine(FileConstants.CsvHeaderWithoutId);

            foreach (var result in results.OrderBy(r => r.Timestamp))
            {
                var timestamp = result.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                var executionTime = result.ExecutionTimeMs.ToString("F3", CultureInfo.InvariantCulture);
                var errorMsg = result.IsError ? $"\"{result.ErrorMessage?.Replace("\"", "\"\"")}\"" : "";

                // На случай если придется выводить всё одинакого
                /*writer.WriteLine($"{executionTime},{result.AccountId}," +
                                 $"{result.ThreadId},{timestamp},{result.IsError},{errorMsg}");*/

                if (hasPercentMarkers)
                    writer.WriteLine($"{executionTime},{result.AccountId}," +
                                     $"{result.ThreadId},{timestamp},{result.IsError},{errorMsg}");
                else
                    writer.WriteLine($"{executionTime}," +
                                     $"{result.ThreadId},{timestamp},{result.IsError},{errorMsg}");
            }

            var successCount = results.Count(r => !r.IsError);
            var errorCount = results.Count(r => r.IsError);

            _dialogService.ShowInfo(
                AppMessages.TestCompleteMessage(results.Count, successCount, errorCount, filePath),
                AppMessages.TestCompleteTitle);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(
                string.Format(AppMessages.SaveError, ex.Message),
                AppMessages.ErrorTitle);
        }
    }
}
