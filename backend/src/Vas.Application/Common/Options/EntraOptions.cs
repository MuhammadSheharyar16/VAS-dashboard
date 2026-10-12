using System.ComponentModel.DataAnnotations;

namespace Vas.Application.Common.Options;

/// <summary>
/// Bound from the same "AzureAd" section that Microsoft.Identity.Web reads. None of these values are secrets.
/// </summary>
public sealed class EntraOptions
{
    public const string SectionName = "AzureAd";

    private const string PlaceholderId = "00000000-0000-0000-0000-000000000000";

    [Required, Url]
    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    [Required]
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Client ID of the protected API app registration.</summary>
    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Client ID of the public SPA app registration.</summary>
    [Required]
    public string SpaClientId { get; init; } = string.Empty;

    [Required]
    public string RequiredScope { get; init; } = "Dashboard.Read";

    public string Authority => $"{Instance.TrimEnd('/')}/{TenantId}";

    public string ApiScope => $"api://{ClientId}/{RequiredScope}";

    public bool HasPlaceholderIds() =>
        TenantId == PlaceholderId || ClientId == PlaceholderId || SpaClientId == PlaceholderId;
}
