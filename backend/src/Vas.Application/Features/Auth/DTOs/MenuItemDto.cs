namespace Vas.Application.Features.Auth.DTOs;

public sealed record MenuItemDto(string Code, string Title, string Route, int Order, bool IsComingSoon);
