using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

internal sealed class GetSubscriptionPlansHandler(IMasterUnitOfWork masterUow)
    : IAppRequestHandler<GetSubscriptionPlansQuery, PagedResult<SubscriptionPlanDto>>
{
    public Task<PagedResult<SubscriptionPlanDto>> Handle(
        GetSubscriptionPlansQuery req, CancellationToken ct)
    {
        var query = masterUow.Repository<SubscriptionPlan>().Query();

        var totalItems = query.Count();

        var items = query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .Select(i => i.ToDto())
            .ToArray();

        return Task.FromResult(PagedResult<SubscriptionPlanDto>.Ok(items, totalItems, req.PageSize));
    }
}
