namespace Trackwise.Infrastructure.Integrations.Accounting.Common;

/// <summary>
///     QuickBooks Online endpoint URLs. OAuth endpoints are environment-independent; the Data
///     API base differs between sandbox and production.
/// </summary>
internal static class QboEndpoints
{
    public const string AuthorizeUrl = "https://appcenter.intuit.com/connect/oauth2";
    public const string TokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
    public const string RevokeUrl = "https://developer.api.intuit.com/v2/oauth2/tokens/revoke";

    private const string SandboxApiBase = "https://sandbox-quickbooks.api.intuit.com";
    private const string ProductionApiBase = "https://quickbooks.api.intuit.com";

    /// <summary>Minor version pins the QBO schema; bump deliberately when adopting new fields.</summary>
    public const string MinorVersion = "75";

    public static string ApiBase(string environment) =>
        string.Equals(environment, "production", StringComparison.OrdinalIgnoreCase)
            ? ProductionApiBase
            : SandboxApiBase;

    public static string CompanyResource(string environment, string realmId, string resource) =>
        $"{ApiBase(environment)}/v3/company/{realmId}/{resource}?minorversion={MinorVersion}";

    public static string QueryResource(string environment, string realmId, string query) =>
        $"{ApiBase(environment)}/v3/company/{realmId}/query?minorversion={MinorVersion}&query={Uri.EscapeDataString(query)}";
}
