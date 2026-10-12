using System.ComponentModel.DataAnnotations;

namespace Vas.Application.Common.Options;

/// <summary>Non-secret connection settings. The token comes from the managed identity, never from here.</summary>
public sealed class FabricOptions
{
    public const string SectionName = "Fabric";

    /// <summary>Host name of the SQL analytics endpoint.</summary>
    public string SqlEndpoint { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;

    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; init; } = 30;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SqlEndpoint) && !string.IsNullOrWhiteSpace(Database);
}
