using Vas.Domain.Enums;

namespace Vas.Application.Common.Reporting;

/// <summary>
/// The filters every report accepts. Dates are a half-open range in UTC: from is included,
/// toExclusive is not. Anything left out falls back to the default view.
/// </summary>
public record ReportFilter
{
    public DateOnly? From { get; init; }

    public DateOnly? ToExclusive { get; init; }

    public string? CountryCode { get; init; }

    public string? CentreCode { get; init; }

    public string? VisaTypeCode { get; init; }

    public ComparisonPeriod Compare { get; init; } = ComparisonPeriod.PreviousPeriod;
}
