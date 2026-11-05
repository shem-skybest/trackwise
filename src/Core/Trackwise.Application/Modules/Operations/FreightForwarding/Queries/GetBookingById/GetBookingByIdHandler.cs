using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetBookingByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetBookingByIdQuery, Result<BookingDto>>
{
    public async Task<Result<BookingDto>> Handle(GetBookingByIdQuery req, CancellationToken ct)
    {
        // The booking carries its own locked charges, documents and lane (JSON), so there is no
        // Quotation/TariffRate/ChargeCode graph to load and nothing is priced in memory.
        var booking = await tenantUow.Repository<Booking>()
            .Query()
            .AsNoTracking()
            .Include(b => b.Charges)
            .Include(b => b.Documents)
            .FirstOrDefaultAsync(b => b.Id == req.Id, ct);

        if (booking is null)
            return Result<BookingDto>.Fail($"Booking with ID '{req.Id}' not found");

        // Read without tracking, so nothing lazy-loads: each lookup below is one batched query.
        var customerName = await tenantUow.Repository<Customer>().Query().AsNoTracking()
            .Where(c => c.Id == booking.CustomerId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct) ?? "Unknown";

        var locationIds = booking.Lane.Stops.Select(s => s.LocationId).Distinct().ToList();
        var locationNames = await tenantUow.Repository<Location>().Query().AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Name, ct);

        var bookingId = booking.Id;
        var billingInvoices = await tenantUow.Repository<BookingInvoice>().Query().AsNoTracking()
            .Where(i => i.BookingId == bookingId)
            .OrderByDescending(i => i.Number)
            .Select(i => new BookingInvoiceSummaryDto
            {
                Id = i.Id,
                Number = i.Number,
                Status = i.Status,
                Purpose = i.Purpose,
                RelatedInvoiceId = i.RelatedInvoiceId
            })
            .ToListAsync(ct);

        var trips = await tenantUow.Repository<Trip>().Query().AsNoTracking()
            .Where(t => t.Stops.Any(s => s.Load.BookingId == bookingId))
            .OrderByDescending(t => t.Number)
            .Select(t => new BookingTripDto { TripId = t.Id, Number = t.Number, Name = t.Name, Status = t.Status })
            .ToListAsync(ct);

        return Result<BookingDto>.Ok(booking.ToDto(customerName, locationNames, trips, billingInvoices));
    }
}
