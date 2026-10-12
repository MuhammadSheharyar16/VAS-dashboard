using FluentValidation;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.Centres.DTOs;

namespace Vas.Application.Features.Centres.Validators;

public sealed class CentresQueryParamsValidator : ReportFilterValidator<CentresQueryParams>
{
    public CentresQueryParamsValidator()
    {
        // Sorting is by a fixed list of fields. Nothing the client sends is ever used as a column name.
        RuleFor(query => query.SortBy)
            .Must(sortBy => CentreSortFields.All.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Sort by one of: {string.Join(", ", CentreSortFields.All)}.")
            .When(query => query.SortBy is not null);

        RuleFor(query => query.SortDirection).IsInEnum();
    }
}
