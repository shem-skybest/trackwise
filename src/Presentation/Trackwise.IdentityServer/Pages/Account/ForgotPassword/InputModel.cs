using System.ComponentModel.DataAnnotations;

namespace Trackwise.IdentityServer.Pages.Account.ForgotPassword;

public class InputModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
