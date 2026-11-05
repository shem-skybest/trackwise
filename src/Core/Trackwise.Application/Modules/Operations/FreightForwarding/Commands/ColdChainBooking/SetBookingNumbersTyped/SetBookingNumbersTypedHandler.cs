using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetBookingNumbersTypedHandler(
    ITenantUnitOfWork tenantUow,
    IMasterUnitOfWork masterUow,
    IWorkfileReferenceGenerator referenceGenerator)
    : IAppRequestHandler<SetBookingNumbersTypedCommand, Result<BookingNumberingDto>>
{
    public async Task<Result<BookingNumberingDto>> Handle(SetBookingNumbersTypedCommand req, CancellationToken ct)
    {
        var tenant = await masterUow.Repository<Tenant>().GetByIdAsync(tenantUow.GetCurrentTenant().Id, ct);
        if (tenant is null)
        {
            return Result<BookingNumberingDto>.Fail("The company could not be found.");
        }

        tenant.Settings = tenant.Settings with { ColdChainBookingNumbersTyped = req.Typed };
        masterUow.Repository<Tenant>().Update(tenant);
        await masterUow.SaveChangesAsync(ct);

        var counter = await referenceGenerator.GetCounterAsync(WorkfileSeries.Reefer, ct);
        return Result<BookingNumberingDto>.Ok(GetBookingNumberingHandler.ToDto(counter, req.Typed));
    }
}
