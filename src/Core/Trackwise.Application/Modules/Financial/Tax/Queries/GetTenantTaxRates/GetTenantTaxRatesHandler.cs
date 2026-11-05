using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Tax.Queries;

internal sealed class GetTenantTaxRatesHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTenantTaxRatesQuery, Result<IReadOnlyList<TenantTaxRateDto>>>
{
    public async Task<Result<IReadOnlyList<TenantTaxRateDto>>> Handle(
        GetTenantTaxRatesQuery req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();
        var rates = await masterUow.Repository<TenantTaxRate>()
            .GetListAsync(r => r.TenantId == tenant.Id, ct);

        var dtos = rates
            .OrderBy(r => r.Jurisdiction.CountryCode)
            .ThenBy(r => r.Jurisdiction.Region)
            .ThenByDescending(r => r.EffectiveFrom)
            .ToDto()
            .ToList();

        return Result<IReadOnlyList<TenantTaxRateDto>>.Ok(dtos);
    }
}
