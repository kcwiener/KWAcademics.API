namespace TextToSpeechConverter.Api.Configuration;

/// <summary>
/// Configuration options for Microsoft Entra External ID authentication
/// </summary>
public class AzureAdOptions
{
    public const string SectionName = "AzureAd";

    /// <summary>
    /// Azure AD instance (e.g., "https://login.microsoftonline.com/")
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD domain (e.g., "yourtenant.onmicrosoft.com")
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID (GUID)
    /// </summary>
  public string TenantId { get; set; } = string.Empty;

    /// <summary>
 /// Client ID of the API application
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// API scopes (e.g., "access_api")
    /// </summary>
    public string Scopes { get; set; } = string.Empty;
}
