using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetBookingNumberingHandler(
    ITenantUnitOfWork tenantUow,
    IWorkfileReferenceGenerator referenceGenerator)
    : IAppRequestHandler<GetBookingNumberingQuery, Result<BookingNumberingDto>>
{
    public async Task<Result<BookingNumberingDto>> Handle(GetBookingNumberingQuery req, CancellationToken ct)
    {
        var counter = await referenceGenerator.GetCounterAsync(WorkfileSeries.Reefer, ct);
        return Result<BookingNumberingDto>.Ok(ToDto(counter, tenantUow.GetCurrentTenant().Settings.ColdChainBookingNumbersTyped));
    }

    internal static BookingNumberingDto ToDto(ReferenceCounter counter, bool typed) => new()
    {
        Typed = typed,
        CounterKey = counter.Key,
        LastIssued = counter.LastAllocated,
        NextNumber = counter.LastAllocated + 1,
        NextReference = counter.NextReference
    };
}
