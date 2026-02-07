using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SqlPerformanceTester.Models;
using SqlPerformanceTester.Services;
using SqlPerformanceTester.Services.Interfaces;
using SqlPerformanceTester.Validators;
using SqlPerformanceTester.ViewModels;

namespace SqlPerformanceTester.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ITestExecutionService, TestExecutionService>();
        services.AddSingleton<IResultSaveService, ResultSaveService>();

        services.AddTransient<IValidator<TestConfiguration>, TestConfigurationValidator>();

        services.AddTransient<MainViewModel>();

        return services;
    }
}
