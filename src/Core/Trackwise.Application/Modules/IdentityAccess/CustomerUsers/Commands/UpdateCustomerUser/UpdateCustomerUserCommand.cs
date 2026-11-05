using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.CustomerUsers.Commands;

/// <summary>
/// Command to update a customer portal user.
/// </summary>
public class UpdateCustomerUserCommand : ICommand<Result>
{
    /// <summary>
    /// The customer user ID to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Optional new display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Whether the portal access is active.
    /// </summary>
    public bool? IsActive { get; set; }
}
