using Trackwise.Application.Modules.Compliance.Eld.Services;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Eld.Queries;

internal sealed class GetHosLimitsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetHosLimitsQuery, Result<HosLimitsDto>>
{
    public Task<Result<HosLimitsDto>> Handle(GetHosLimitsQuery req, CancellationToken ct)
    {
        var region = tenantUow.GetCurrentTenant().Settings.Region;
        var dto = RuleSetSelector.LimitsFor(region).ToDto();
        return Task.FromResult(Result<HosLimitsDto>.Ok(dto));
    }
}
