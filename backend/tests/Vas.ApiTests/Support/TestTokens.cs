using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Vas.Shared.Constants;

namespace Vas.ApiTests.Support;

/// <summary>
/// Mints Entra-shaped access tokens signed with a key generated for this test run.
/// The API validates them with its real pipeline: signature, issuer, audience, expiry, scope and role.
/// </summary>
public static class TestTokens
{
    public const string Issuer = "https://test-issuer.local/v2.0";
    public const string TenantId = "11111111-1111-1111-1111-111111111111";
    public const string ApiClientId = "22222222-2222-2222-2222-222222222222";
    public const string SpaClientId = "33333333-3333-3333-3333-333333333333";
    public const string Scope = "Dashboard.Read";
    public const string UserObjectId = "44444444-4444-4444-4444-444444444444";

    public static RsaSecurityKey SigningKey { get; } = new(RSA.Create(2048)) { KeyId = "test-key" };

    private static RsaSecurityKey UntrustedKey { get; } = new(RSA.Create(2048)) { KeyId = "untrusted-key" };

    public static string Viewer() => Create(roles: [AppRoles.Viewer]);

    public static string Create(
        string[]? roles = null,
        string? scope = Scope,
        string audience = ApiClientId,
        DateTime? expires = null,
        bool signWithUntrustedKey = false)
    {
        var claims = new List<Claim>
        {
            new("oid", UserObjectId),
            new("tid", TenantId),
            new("ver", "2.0"),
            new("name", "Test Executive"),
            new("preferred_username", "executive@example.test"),
        };

        if (scope is not null)
        {
            claims.Add(new Claim("scp", scope));
        }

        claims.AddRange((roles ?? []).Select(role => new Claim("roles", role)));

        var expiresAt = expires ?? DateTime.UtcNow.AddMinutes(10);

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = expiresAt.AddMinutes(-20),
            IssuedAt = expiresAt.AddMinutes(-20),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                signWithUntrustedKey ? UntrustedKey : SigningKey,
                SecurityAlgorithms.RsaSha256),
        });
    }
}
