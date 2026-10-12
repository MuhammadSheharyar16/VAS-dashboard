using Vas.Domain.Enums;

namespace Vas.Application.Common.Reporting;

/// <summary>The filters after defaults and lookup checks, ready to apply to a query.</summary>
public sealed record ReportScope(
    DateRange Current,
    DateRange? Previous,
    string? CountryCode,
    string? CentreCode,
    string? VisaTypeCode);

public sealed record ReportContext(ReportScope Scope, ReportMetaDto Meta);

/// <summary>Travels with every report so the client can show what it is looking at.</summary>
public sealed record ReportMetaDto(
    DateTime GeneratedAtUtc,
    ReportingDataSource DataSource,
    DataFreshnessDto Freshness,
    AppliedFilterDto Filter,
    bool ThresholdsArePlaceholders);

public sealed record DataFreshnessDto(DateTime? LastRefreshedAtUtc);

public sealed record AppliedFilterDto(
    DateOnly From,
    DateOnly ToExclusive,
    string? CountryCode,
    string? CentreCode,
    string? VisaTypeCode,
    ComparisonPeriod Compare,
    DateOnly? PreviousFrom,
    DateOnly? PreviousToExclusive);
