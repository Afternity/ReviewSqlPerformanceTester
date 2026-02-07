using SqlPerformanceTester.Models;

namespace SqlPerformanceTester.Services.Interfaces;

public interface ITestExecutionService
{
    Task<IReadOnlyCollection<TestResult>> RunLoadTestAsync(
        TestConfiguration config,
        Action<string> updateCountdown,
        CancellationToken cancellationToken);
}
