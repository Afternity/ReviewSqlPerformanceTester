namespace SqlPerformanceTester.Models;

public class TestConfiguration
{
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int ThreadsCount { get; set; }
    public int TestDuration { get; set; }
    public string OutputFile { get; set; } = string.Empty;
}
