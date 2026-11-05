namespace Trackwise.Application.Abstractions.Email.Models;

public class PasswordResetEmailModel
{
    public string ResetUrl { get; set; } = string.Empty;
}
