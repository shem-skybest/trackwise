using Trackwise.Application.Abstractions.Accounting;
using Microsoft.AspNetCore.DataProtection;

namespace Trackwise.Infrastructure.Persistence.Services.Accounting;

public sealed class OAuthStateProtector(IDataProtectionProvider provider) : IOAuthStateProtector
{
    private const string Purpose = "Trackwise.OAuthState.v1";
    private readonly IDataProtector protector = provider.CreateProtector(Purpose);

    public string Protect(Guid tenantId) => protector.Protect(tenantId.ToString());

    public Guid? TryUnprotect(string state)
    {
        try
        {
            return Guid.Parse(protector.Unprotect(state));
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException)
        {
            return null;
        }
    }
}
