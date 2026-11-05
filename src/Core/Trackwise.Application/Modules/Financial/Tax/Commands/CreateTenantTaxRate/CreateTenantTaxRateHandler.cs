using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Tax.Commands;

internal sealed class CreateTenantTaxRateHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateTenantTaxRateCommand, Result<TenantTaxRateDto>>
{
    public async Task<Result<TenantTaxRateDto>> Handle(CreateTenantTaxRateCommand req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();
        var rate = new TenantTaxRate
        {
            TenantId = tenant.Id,
            Jurisdiction = new TaxJurisdiction
            {
                CountryCode = req.CountryCode.ToUpperInvariant(),
                Region = string.IsNullOrWhiteSpace(req.Region) ? null : req.Region.ToUpperInvariant()
            },
            RatePercent = req.RatePercent,
            Description = req.Description,
            EffectiveFrom = req.EffectiveFrom ?? DateTime.UtcNow,
            EffectiveTo = req.EffectiveTo
        };

        await masterUow.Repository<TenantTaxRate>().AddAsync(rate, ct);
        await masterUow.SaveChangesAsync(ct);

        return Result<TenantTaxRateDto>.Ok(rate.ToDto());
    }
}
