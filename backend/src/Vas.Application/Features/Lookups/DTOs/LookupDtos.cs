namespace Vas.Application.Features.Lookups.DTOs;

/// <summary>One choice in a filter dropdown. <see cref="Group"/> is the region, the country or the category.</summary>
public sealed record LookupItemDto(string Code, string Name, string? Group);

public sealed record CentreLookupQueryParams
{
    /// <summary>Limits the centres to one country.</summary>
    public string? CountryCode { get; init; }
}
