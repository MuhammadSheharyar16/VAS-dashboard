using FluentValidation;

namespace Vas.Application.Common.Reporting;

/// <summary>
/// Shape rules shared by every report query. Whether a code exists is checked afterwards against
/// the lookups, by <see cref="ReportContextService"/>.
/// </summary>
public abstract class ReportFilterValidator<T> : AbstractValidator<T>
    where T : ReportFilter
{
    public const int MaxCodeLength = 20;

    private const string CodePattern = "^[A-Za-z0-9_-]+$";
    private const string CodeMessage = "Use letters, digits, '-' and '_' only.";

    protected ReportFilterValidator()
    {
        RuleFor(filter => filter.CountryCode).MaximumLength(MaxCodeLength).Matches(CodePattern).WithMessage(CodeMessage);
        RuleFor(filter => filter.CentreCode).MaximumLength(MaxCodeLength).Matches(CodePattern).WithMessage(CodeMessage);
        RuleFor(filter => filter.VisaTypeCode).MaximumLength(MaxCodeLength).Matches(CodePattern).WithMessage(CodeMessage);
        RuleFor(filter => filter.Compare).IsInEnum();

        RuleFor(filter => filter.From)
            .Custom((_, context) =>
            {
                var filter = context.InstanceToValidate;
                var problem = DateRange.Validate(filter.From!.Value, filter.ToExclusive!.Value);
                if (problem is not null)
                {
                    context.AddFailure(nameof(ReportFilter.From), problem);
                }
            })
            .When(filter => filter.From.HasValue && filter.ToExclusive.HasValue);
    }
}
