using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetNextBookingNumberHandler(
    ITenantUnitOfWork tenantUow,
    IWorkfileReferenceGenerator referenceGenerator)
    : IAppRequestHandler<SetNextBookingNumberCommand, Result<BookingNumberingDto>>
{
    public async Task<Result<BookingNumberingDto>> Handle(SetNextBookingNumberCommand req, CancellationToken ct)
    {
        if (!await referenceGenerator.TrySetNextAsync(WorkfileSeries.Reefer, req.NextNumber, ct))
        {
            var counter = await referenceGenerator.GetCounterAsync(WorkfileSeries.Reefer, ct);
            return Result<BookingNumberingDto>.Fail(
                $"Bookings up to number {counter.LastAllocated} have already been issued this year, so the next one " +
                $"can't be {req.NextNumber} - it would be issued twice. The lowest it can be is {counter.LastAllocated + 1}.");
        }

        var updated = await referenceGenerator.GetCounterAsync(WorkfileSeries.Reefer, ct);
        return Result<BookingNumberingDto>.Ok(GetBookingNumberingHandler.ToDto(
            updated, tenantUow.GetCurrentTenant().Settings.ColdChainBookingNumbersTyped));
    }
}
