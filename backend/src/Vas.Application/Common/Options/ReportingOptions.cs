using Vas.Domain.Enums;

namespace Vas.Application.Common.Options;

public sealed class ReportingOptions
{
    public const string SectionName = "Reporting";

    /// <summary>Mock serves the embedded JSON files; Fabric reads the SQL analytics endpoint.</summary>
    public ReportingDataSource DataSource { get; init; } = ReportingDataSource.Fabric;
}
