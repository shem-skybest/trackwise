using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Trackwise.IdentityServer.Pages.ExternalLogin;

[SecurityHeaders]
[AllowAnonymous]
public class NoAccount : PageModel
{
    public void OnGet()
    {
    }
}
