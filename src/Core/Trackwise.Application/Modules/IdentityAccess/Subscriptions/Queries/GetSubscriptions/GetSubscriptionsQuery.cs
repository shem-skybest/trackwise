using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

public sealed class GetSubscriptionsQuery : SearchableQuery, IQuery<PagedResult<SubscriptionDto>>
{
}
