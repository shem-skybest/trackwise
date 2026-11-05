using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

internal sealed class
    GetSubscriptionPlanHandler : IAppRequestHandler<GetSubscriptionPlanQuery, Result<SubscriptionPlanDto>>
{
    private readonly IMasterUnitOfWork _masterUow;

    public GetSubscriptionPlanHandler(IMasterUnitOfWork masterUow)
    {
        _masterUow = masterUow;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(
        GetSubscriptionPlanQuery req, CancellationToken ct)
    {
        var entity = await _masterUow.Repository<SubscriptionPlan>().GetAsync(i => i.Id == req.Id);

        if (entity is null)
        {
            return Result<SubscriptionPlanDto>.Fail($"Could not find a subscription plan with ID '{req.Id}'");
        }

        var dto = entity.ToDto();
        return Result<SubscriptionPlanDto>.Ok(dto);
    }
}
