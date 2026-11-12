namespace Trackwise.Shared.Models;

/// <summary>
/// The company's Xendit connection as shown in Settings. Secrets never leave the API; only
/// the last characters of the key are shown so staff can tell which key is saved.
/// </summary>
public record XenditSettingsDto
{
    public bool IsConfigured { get; set; }
    public bool IsActive { get; set; }

    /// <summary>True for a sandbox (<c>xnd_development_</c>) key - payments are not real.</summary>
    public bool IsTestMode { get; set; }

    /// <summary>e.g. <c>xnd_production_…a1b2</c>.</summary>
    public string? SecretKeyHint { get; set; }
}
