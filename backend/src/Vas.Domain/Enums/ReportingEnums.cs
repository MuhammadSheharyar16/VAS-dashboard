namespace Vas.Domain.Enums;

public enum ReportingDataSource
{
    Mock,
    Fabric,
}

public enum ComparisonPeriod
{
    None,
    PreviousPeriod,
}

public enum KpiStatus
{
    Neutral,
    Good,
    Warning,
    Critical,
}

public enum KpiUnit
{
    Count,
    Percent,
    Days,
    Currency,
}

public enum TrendDirection
{
    Flat,
    Up,
    Down,
}

public enum TrendGranularity
{
    Day,
    Month,
}
