using System.ComponentModel.DataAnnotations;

namespace Vas.Application.Common.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 10_000)]
    public int PermitLimit { get; init; } = 120;

    [Range(1, 3_600)]
    public int WindowSeconds { get; init; } = 60;
}
