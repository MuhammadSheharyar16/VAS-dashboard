namespace Vas.Application.Common.Options;

public sealed class KpiThreshold
{
    public decimal Warning { get; init; }

    public decimal Critical { get; init; }

    /// <summary>True when a larger value is better (approval rate); false when smaller is better (turnaround).</summary>
    public bool HigherIsBetter { get; init; }

    public bool IsConsistent => HigherIsBetter ? Warning > Critical : Warning < Critical;
}

public sealed class KpiThresholdOptions
{
    public const string SectionName = "KpiThresholds";

    /// <summary>True until AEGI confirms the real thresholds. Reported to the client with every response.</summary>
    public bool ArePlaceholders { get; init; } = true;

    /// <summary>Keyed by KPI key. A KPI without an entry has no status.</summary>
    public Dictionary<string, KpiThreshold> Kpis { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
