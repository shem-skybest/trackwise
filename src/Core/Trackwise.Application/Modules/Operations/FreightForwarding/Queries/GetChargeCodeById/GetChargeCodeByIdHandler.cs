using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetChargeCodeByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetChargeCodeByIdQuery, Result<ChargeCodeDto>>
{
    public async Task<Result<ChargeCodeDto>> Handle(GetChargeCodeByIdQuery req, CancellationToken ct)
    {
        var chargeCode = await tenantUow.Repository<ChargeCode>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        return chargeCode is not null
            ? Result<ChargeCodeDto>.Ok(chargeCode.ToDto())
            : Result<ChargeCodeDto>.Fail($"Charge code with ID '{req.Id}' not found");
    }
}
