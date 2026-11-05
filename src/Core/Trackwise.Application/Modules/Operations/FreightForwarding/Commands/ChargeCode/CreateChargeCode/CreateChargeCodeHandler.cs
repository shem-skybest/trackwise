using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using MediatR;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CreateChargeCodeHandler(ITenantUnitOfWork tenantUow)
    : IRequestHandler<CreateChargeCodeCommand, Result<ChargeCodeDto>>
{
    public async Task<Result<ChargeCodeDto>> Handle(CreateChargeCodeCommand req, CancellationToken ct)
    {
        var code = req.Code.ToUpperInvariant();
        var existing = await tenantUow.Repository<ChargeCode>().GetAsync(i => i.Code == code, ct);
        if (existing is not null)
        {
            return Result<ChargeCodeDto>.Fail($"A charge code with code '{code}' already exists");
        }

        var chargeCode = ChargeCode.Create(
            code,
            req.Name,
            req.Category,
            req.ApplicableModes,
            req.DefaultUnit,
            req.RequiresApproval,
            req.Description,
            req.TaxCode);

        await tenantUow.Repository<ChargeCode>().AddAsync(chargeCode, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<ChargeCodeDto>.Ok(chargeCode.ToDto());
    }
}
