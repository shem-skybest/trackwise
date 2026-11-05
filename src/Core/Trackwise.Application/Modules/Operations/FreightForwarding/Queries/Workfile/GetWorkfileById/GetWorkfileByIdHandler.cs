using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetWorkfileByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetWorkfileByIdQuery, Result<WorkfileDto>>
{
    public async Task<Result<WorkfileDto>> Handle(GetWorkfileByIdQuery req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == req.Id, ct);

        if (workfile is null)
        {
            return Result<WorkfileDto>.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        var customer = await tenantUow.Repository<Customer>().GetByIdAsync(workfile.CustomerId, ct);

        long? bookingNumber = null;
        if (workfile.BookingId.HasValue)
        {
            bookingNumber = await tenantUow.Repository<Booking>().Query()
                .AsNoTracking()
                .Where(b => b.Id == workfile.BookingId.Value)
                .Select(b => (long?)Convert.ToInt64(b.BookingNumber))
                .FirstOrDefaultAsync(ct);
        }

        // Batched: reading each partner through its navigation property would be five lazy loads.
        var partnerIds = new[]
            {
                workfile.ShippingLineId, workfile.CoLoaderId, workfile.ForwarderId,
                workfile.TruckerId, workfile.BrokerId
            }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var partnerNames = partnerIds.Length == 0
            ? []
            // Past the soft-delete filter, and that is not optional. Partners became master data and
            // can now be retired; a file that named one still has to render its name, or the
            // counterparty on a closed file silently becomes blank. This is a display path - the
            // check that a file may *use* a partner is WorkfilePartnerValidation's, and that one
            // reads the deletion deliberately.
            : await tenantUow.Repository<BusinessPartner>().Query()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => partnerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var gensetName = workfile.GensetId.HasValue
            ? await tenantUow.Repository<Genset>().Query()
                .AsNoTracking()
                .Where(g => g.Id == workfile.GensetId.Value)
                .Select(g => (string?)g.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var containers = await WorkfileContainerReader.ReadAsync(tenantUow, workfile.Id, ct);
        var legs = await TruckLegReader.ReadAsync(tenantUow, workfile, containers, ct);
        var timeline = workfile is RegularWorkfile forwarding
            ? await ContainerTimelineReader.ReadAsync(tenantUow, forwarding, containers, ct)
            : null;

        return Result<WorkfileDto>.Ok(workfile.ToDto(
            customer?.Name ?? string.Empty, bookingNumber, partnerNames, gensetName, containers, legs, timeline));
    }
}
