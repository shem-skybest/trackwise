using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Commands;

public class DeleteSubscriptionPlanCommand : ICommand, IHaveId
{
    public Guid Id { get; set; }
}
