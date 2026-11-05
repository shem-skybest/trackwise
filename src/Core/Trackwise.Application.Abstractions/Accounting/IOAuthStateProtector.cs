namespace Trackwise.Application.Abstractions.Accounting;

/// <summary>
///     Protects the tenant identity carried in the OAuth2 <c>state</c> parameter so the
///     anonymous provider callback cannot be forged. Backed by ASP.NET Core Data Protection.
/// </summary>
public interface IOAuthStateProtector
{
    /// <summary>Produce a tamper-proof state string encoding the tenant id.</summary>
    string Protect(Guid tenantId);

    /// <summary>Recover the tenant id from a state string, or null if invalid/tampered.</summary>
    Guid? TryUnprotect(string state);
}
