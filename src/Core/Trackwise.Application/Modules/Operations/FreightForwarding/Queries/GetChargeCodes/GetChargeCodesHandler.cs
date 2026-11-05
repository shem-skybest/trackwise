using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetChargeCodesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetChargeCodesQuery, Result<IEnumerable<ChargeCodeDto>>>
{
    public async Task<Result<IEnumerable<ChargeCodeDto>>> Handle(
        GetChargeCodesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<ChargeCode>().Query();

        if (req.OnlyActive)
        {
            query = query.Where(i => i.IsActive);
        }

        var chargeCodes = await query.ToListAsync(ct);
        return Result<IEnumerable<ChargeCodeDto>>.Ok(chargeCodes.ToDtoList());
    }
}
