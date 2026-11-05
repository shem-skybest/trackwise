using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.RateAgreements;

/// <summary>Builds the full agreement DTO - lines and their surcharges - in three queries.</summary>
internal static class RateAgreementReader
{
    public static async Task<RateAgreementDto> ToDtoAsync(
        ITenantUnitOfWork tenantUow, RateAgreement agreement, CancellationToken ct)
    {
        var lineIds = agreement.Lines.Select(l => l.Id).ToList();

        var surcharges = await tenantUow.Repository<RateFuelSurcharge>().Query()
            .Where(s => lineIds.Contains(s.RateAgreementLineId))
            .ToListAsync(ct);

        var customerName = await tenantUow.Repository<Customer>().Query()
            .Where(c => c.Id == agreement.CustomerId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        return agreement.ToDto(customerName, surcharges.ToLookup(s => s.RateAgreementLineId));
    }
}
