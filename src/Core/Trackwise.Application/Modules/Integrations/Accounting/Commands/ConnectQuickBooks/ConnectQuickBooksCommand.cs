using Trackwise.Application.Abstractions.Common;

namespace Trackwise.Application.Modules.Integrations.Accounting.Commands;

/// <summary>
///     Completes the QuickBooks Online OAuth2 flow from the redirect callback: resolves the
///     tenant from the protected <see cref="State"/>, exchanges the code for tokens, resolves
///     default accounts, and persists the connection. Not feature-gated at the pipeline level -
///     the callback is anonymous and sets the tenant context itself from <see cref="State"/>.
/// </summary>
public class ConnectQuickBooksCommand : ICommand
{
    public required string Code { get; set; }
    public required string RealmId { get; set; }
    public required string State { get; set; }
}
