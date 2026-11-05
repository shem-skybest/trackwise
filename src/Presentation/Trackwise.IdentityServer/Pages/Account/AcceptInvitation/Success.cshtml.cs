using Trackwise.Domain.Primitives.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Trackwise.IdentityServer.Pages.Account.AcceptInvitation;

[AllowAnonymous]
public class Success(IConfiguration configuration) : PageModel
{
    public string? TenantName { get; set; }
    public string? Role { get; set; }
    public string PortalUrl { get; set; } = "/";

    // Only the invitation type travels in the query string; the URL comes from config, so a
    // crafted link cannot point the button at another site.
    public void OnGet(string? tenantName, string? role, InvitationType? type)
    {
        TenantName = tenantName;
        Role = role;

        var portalKey = type switch
        {
            InvitationType.CustomerUser => "Customer",
            InvitationType.AppUser => "Admin",
            _ => "Tms"
        };
        PortalUrl = configuration[$"Portals:{portalKey}"] ?? "/";
    }
}
