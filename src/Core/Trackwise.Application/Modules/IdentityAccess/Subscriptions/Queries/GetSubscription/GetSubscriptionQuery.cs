using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

public sealed class GetSubscriptionQuery : IQuery<Result<SubscriptionDto>>
{
    public Guid Id { get; set; }
}
