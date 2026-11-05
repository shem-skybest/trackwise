using System.ComponentModel.DataAnnotations;

namespace Trackwise.IdentityServer.Pages.Account.Manage.Email;

public class InputModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "New email")]
    public string NewEmail { get; set; }
}
