using System.ComponentModel.DataAnnotations;

namespace Vas.Application.Common.Options;

/// <summary>
/// Bound from the same "AzureAd" section that Microsoft.Identity.Web reads. None of these values are secrets.
/// </summary>
public sealed class EntraOptions
{
    public const string SectionName = "AzureAd";

    public const string MultiTenantAuthority = "organizations";

    private const string PlaceholderId = "00000000-0000-0000-0000-000000000000";

    [Required, Url]
    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// Optional. Set it to limit sign-in to this tenant; leave it and <see cref="AllowedTenantIds"/> empty
    /// to accept any Entra organisation whose user signs in and holds the role.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Optional. More tenants to limit sign-in to, as comma-separated tenant IDs.</summary>
    public string AllowedTenantIds { get; init; } = string.Empty;

    /// <summary>Client ID of the protected API app registration.</summary>
    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Client ID of the public SPA app registration.</summary>
    [Required]
    public string SpaClientId { get; init; } = string.Empty;

    [Required]
    public string RequiredScope { get; init; } = "Dashboard.Read";

    /// <summary>The tenants sign-in is limited to. Empty means any Entra organisation is accepted.</summary>
    public IReadOnlyCollection<string> AllowedTenants =>
        AdditionalTenants()
            .Prepend(TenantId.Trim())
            .Where(tenantId => tenantId.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>
    /// The tenant part of the authority: the one allowed tenant, otherwise the endpoint for any work or school account.
    /// </summary>
    public string AuthorityTenant => AllowedTenants.Count == 1 ? AllowedTenants.Single() : MultiTenantAuthority;

    public string Authority => $"{Instance.TrimEnd('/')}/{AuthorityTenant}";

    public string ApiScope => $"api://{ClientId}/{RequiredScope}";

    public bool HasPlaceholderIds() =>
        TenantId == PlaceholderId || ClientId == PlaceholderId || SpaClientId == PlaceholderId;

    public bool HasValidAllowedTenantIds() =>
        AdditionalTenants().All(tenantId => Guid.TryParse(tenantId, out var id) && id != Guid.Empty);

    private string[] AdditionalTenants() =>
        AllowedTenantIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
