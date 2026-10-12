namespace Vas.Application.Features.Auth.DTOs;

public sealed record CurrentUserDto(
    string Id,
    string? DisplayName,
    string? Username,
    IReadOnlyCollection<string> Roles);
