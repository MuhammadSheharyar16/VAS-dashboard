namespace Vas.Application.Features.Auth.DTOs;

public sealed record PermissionDto(string Screen, IReadOnlyList<string> Actions);
