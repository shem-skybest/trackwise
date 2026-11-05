using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Commands;

public class RenewSubscriptionCommand : ICommand
{
    public Guid Id { get; set; }
}
