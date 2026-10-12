namespace Vas.Application.Features.Auth.DTOs;

/// <summary>Non-secret sign-in settings the SPA loads from /config.json before it can call MSAL.</summary>
public sealed record ClientConfigDto(string TenantId, string ClientId, string Authority, string ApiScope);
