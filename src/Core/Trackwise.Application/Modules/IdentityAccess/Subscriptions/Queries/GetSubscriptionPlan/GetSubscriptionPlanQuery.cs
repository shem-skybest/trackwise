using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

public sealed class GetSubscriptionPlanQuery : IQuery<Result<SubscriptionPlanDto>>
{
    public Guid Id { get; set; }
}
