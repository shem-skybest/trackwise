using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

internal sealed class TariffRateResolver(ITenantUnitOfWork tenantUow) : ITariffRateResolver
{
    public async Task<TariffRate?> ResolveRateAsync(
        Guid? customerId,
        Guid? contractId,
        string chargeCode,
        FreightMode mode,
        string? originLane = null,
        string? destinationLane = null,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = tenantUow.Repository<TariffRate>().Query()
            .Include(r => r.Tariff)
            .Include(r => r.ChargeCode)
            .Where(r => r.Tariff!.IsActive &&
                        r.ChargeCode!.IsActive &&
                        r.ChargeCode.Code == chargeCode &&
                        r.Tariff.EffectiveFrom <= today &&
                        (!r.Tariff.EffectiveTo.HasValue || r.Tariff.EffectiveTo.Value >= today));

        if (customerId.HasValue)
        {
            var clientRate = await query
                .Where(r => r.Tariff!.CustomerId == customerId.Value &&
                            r.Tariff.Mode == mode &&
                            (originLane == null || r.Tariff.OriginLane == originLane) &&
                            (destinationLane == null || r.Tariff.DestinationLane == destinationLane))
                .OrderByDescending(r => r.Tariff!.Version)
                .FirstOrDefaultAsync(ct);

            if (clientRate is not null)
                return clientRate;
        }

        // Client-specific tariffs take precedence over contract-specific and general tariffs.
        if (contractId.HasValue)
        {
            var contractedRate = await query
                .Where(r => r.Tariff!.ContractId == contractId.Value &&
                            r.Tariff.CustomerId == null &&
                            r.Tariff.Mode == mode &&
                            (originLane == null || r.Tariff.OriginLane == originLane) &&
                            (destinationLane == null || r.Tariff.DestinationLane == destinationLane))
                .OrderByDescending(r => r.Tariff!.Version)
                .FirstOrDefaultAsync(ct);

            if (contractedRate is not null)
                return contractedRate;
        }

        // Fall back to a general tariff when no client or contract rate matches.
        var standardRate = await query
            .Where(r => r.Tariff!.ContractId == null && r.Tariff.CustomerId == null &&
                        r.Tariff.Mode == mode)
            .Where(r => (originLane == null || r.Tariff!.OriginLane == originLane) &&
                        (destinationLane == null || r.Tariff!.DestinationLane == destinationLane))
            .OrderByDescending(r => r.Tariff!.Version)
            .FirstOrDefaultAsync(ct);

        return standardRate;
    }
}
