namespace SqlPerformanceTester.Common.Constants;

public static class FileConstants
{
    public const string CsvFilter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
    public const string CsvExtension = ".csv";
    public const string DefaultOutputFile = "results.csv";

    public const string CsvHeaderWithId = "ExecutionTimeMs,ID,ThreadId,Timestamp,IsError,ErrorMessage";
    public const string CsvHeaderWithoutId = "ExecutionTimeMs,ThreadId,Timestamp,IsError,ErrorMessage";
}
