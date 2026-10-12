using System.ComponentModel.DataAnnotations;

namespace Vas.Application.Common.Options;

public sealed class PaginationOptions
{
    public const string SectionName = "Pagination";

    [Range(1, 1_000)]
    public int DefaultPageSize { get; init; } = 20;

    [Range(1, 1_000)]
    public int MaxPageSize { get; init; } = 100;
}
