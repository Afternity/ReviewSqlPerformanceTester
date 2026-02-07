namespace SqlPerformanceTester.Models;

public record TestResult
{
    public double ExecutionTimeMs { get; set; }
    public int AccountId { get; set; }
    public int ThreadId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
}
