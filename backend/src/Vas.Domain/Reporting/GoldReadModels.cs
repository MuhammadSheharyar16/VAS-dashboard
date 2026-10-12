namespace Vas.Domain.Reporting;

// Read models for the Fabric Gold objects. The shape is assumed (project-specs.md 3.1) until the
// KPI Mapping arrives; the real object and column names are mapped in the EF configurations.

/// <summary>gold.dim_country: one row per country.</summary>
public sealed class Country
{
    public required string CountryCode { get; init; }
    public required string CountryName { get; init; }
    public required string Region { get; init; }
}

/// <summary>gold.dim_centre: one row per centre.</summary>
public sealed class Centre
{
    public required string CentreCode { get; init; }
    public required string CentreName { get; init; }
    public required string CountryCode { get; init; }
    public required string City { get; init; }
    public required int DailyCapacity { get; init; }
}

/// <summary>gold.dim_visa_type: one row per visa type.</summary>
public sealed class VisaType
{
    public required string VisaTypeCode { get; init; }
    public required string VisaTypeName { get; init; }
    public required string Category { get; init; }
}

/// <summary>gold.fact_visa_application: one row per application.</summary>
public sealed class VisaApplication
{
    public required string ApplicationId { get; init; }
    public required DateOnly SubmittedDate { get; init; }
    public required string CountryCode { get; init; }
    public required string CentreCode { get; init; }
    public required string VisaTypeCode { get; init; }
    public required string Channel { get; init; }
    public required string CurrentStage { get; init; }
    public required string Status { get; init; }
    public required string? Outcome { get; init; }
    public required DateOnly? DecisionDate { get; init; }
    public required int? TurnaroundDays { get; init; }
}

/// <summary>gold.fact_application_stage: one row per application per stage.</summary>
public sealed class ApplicationStage
{
    public required string ApplicationId { get; init; }
    public required string Stage { get; init; }
    public required DateTime EnteredAtUtc { get; init; }
    public required DateTime? ExitedAtUtc { get; init; }
}

/// <summary>gold.fact_vas_revenue: one row per VAS transaction.</summary>
public sealed class VasRevenueTransaction
{
    public required string TransactionId { get; init; }
    public required DateOnly TransactionDate { get; init; }
    public required string CountryCode { get; init; }
    public required string CentreCode { get; init; }
    public required string VisaTypeCode { get; init; }
    public required string ServiceName { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
}

/// <summary>gold.refresh_log: one row per refresh.</summary>
public sealed class RefreshLog
{
    public required DateTime RefreshedAtUtc { get; init; }
    public required string Status { get; init; }
}
