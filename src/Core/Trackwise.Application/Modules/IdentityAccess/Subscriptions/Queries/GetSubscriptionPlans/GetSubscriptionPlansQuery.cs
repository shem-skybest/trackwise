using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

public sealed class GetSubscriptionPlansQuery : SearchableQuery, IQuery<PagedResult<SubscriptionPlanDto>>
{
}
