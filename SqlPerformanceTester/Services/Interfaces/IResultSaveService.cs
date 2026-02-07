using SqlPerformanceTester.Models;

namespace SqlPerformanceTester.Services.Interfaces;

public interface IResultSaveService
{
    void SaveResults(
        IReadOnlyCollection<TestResult> results,
        string queryTemplate,
        string outputFile);
}
