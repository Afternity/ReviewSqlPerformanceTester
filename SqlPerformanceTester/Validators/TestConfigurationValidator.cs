using FluentValidation;
using SqlPerformanceTester.Common.Constants;
using SqlPerformanceTester.Models;

namespace SqlPerformanceTester.Validators;

public class TestConfigurationValidator : AbstractValidator<TestConfiguration>
{
    public TestConfigurationValidator()
    {
        RuleFor(x => x.Server)
            .NotEmpty()
            .WithMessage(ValidationMessages.ServerRequired);

        RuleFor(x => x.Database)
            .NotEmpty()
            .WithMessage(ValidationMessages.DatabaseRequired);

        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage(ValidationMessages.QueryRequired);

        RuleFor(x => x.ThreadsCount)
            .InclusiveBetween(1, 100)
            .WithMessage(ValidationMessages.ThreadsRangeError);

        RuleFor(x => x.TestDuration)
            .GreaterThan(0)
            .WithMessage(ValidationMessages.DurationMinError);

        RuleFor(x => x.OutputFile)
            .NotEmpty()
            .WithMessage(ValidationMessages.OutputFileRequired);
    }
}
