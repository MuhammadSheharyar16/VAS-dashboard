using FluentValidation;
using Vas.Application.Features.Lookups.DTOs;

namespace Vas.Application.Features.Lookups.Validators;

public sealed class CentreLookupQueryParamsValidator : AbstractValidator<CentreLookupQueryParams>
{
    public CentreLookupQueryParamsValidator()
    {
        RuleFor(query => query.CountryCode)
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Use letters, digits, '-' and '_' only.");
    }
}
