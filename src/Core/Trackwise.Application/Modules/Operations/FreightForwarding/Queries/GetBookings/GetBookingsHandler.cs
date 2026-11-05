using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetBookingsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetBookingsQuery, PagedResult<BookingSummaryDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedResult<BookingSummaryDto>> Handle(GetBookingsQuery req, CancellationToken ct)
    {
        var page = Math.Max(req.Page, 1);
        var pageSize = Math.Clamp(req.PageSize, 1, MaxPageSize);

        var query = tenantUow.Repository<Booking>().Query().AsNoTracking();

        if (req.CustomerId is { } customerId)
            query = query.Where(b => b.CustomerId == customerId);

        if (req.Status is { } status)
            query = query.Where(b => b.Status == status);

        if (req.Mode is { } mode)
            query = query.Where(b => b.Mode == mode);

        if (req.FreightMode is { } freightMode)
            query = query.Where(b => b.FreightMode == freightMode);

        if (req.CreditCheckStatus is { } creditStatus)
            query = query.Where(b => b.CreditCheckStatus == creditStatus);

        if (req.PickupFrom is { } from)
            query = query.Where(b => b.RequestedPickupStart >= from);

        if (req.PickupTo is { } to)
            query = query.Where(b => b.RequestedPickupStart <= to);

        if (req.HasUnbilledCharges is { } unbilled)
        {
            query = unbilled
                ? query.Where(b => b.Charges.Any(c => c.BillingStatus == BillingStatus.Unbilled && c.IsBillable))
                : query.Where(b => !b.Charges.Any(c => c.BillingStatus == BillingStatus.Unbilled && c.IsBillable));
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var pattern = $"%{req.Search.Trim()}%";
            query = query.Where(b =>
                EF.Functions.Like(b.BookingNumber, pattern) ||
                EF.Functions.Like(b.QuotationReference, pattern) ||
                (b.CustomerReference != null && EF.Functions.Like(b.CustomerReference, pattern)));
        }

        // Count BEFORE projecting so the count query carries no aggregate sub-selects.
        var totalItems = await query.CountAsync(ct);

        // One SQL statement. Totals come from the booking's locked charge lines (price snapshots),
        // so there is no Quotation/TariffRate/ChargeCode graph to load and no in-memory pricing.
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingSummaryDto
            {
                Id = b.Id,
                BookingNumber = b.BookingNumber,
                Status = b.Status,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer != null ? b.Customer.Name : "Unknown",
                CustomerReference = b.CustomerReference,
                QuotationId = b.QuotationId,
                QuotationReference = b.QuotationReference,
                Currency = b.Currency,

                Mode = b.Mode,
                FreightMode = b.FreightMode,
                ShipmentType = b.ShipmentType,
                RequestedPickupStart = b.RequestedPickupStart,
                RequestedDeliveryEnd = b.RequestedDeliveryEnd,

                TotalSelling = b.Charges.Sum(c => c.SellingAmount),
                UnbilledSelling = b.Charges
                    .Where(c => c.BillingStatus == BillingStatus.Unbilled && c.IsBillable)
                    .Sum(c => c.SellingAmount),
                ChargeCount = b.Charges.Count(),

                CreditCheckStatus = b.CreditCheckStatus,
                OpenDocumentCount = b.Documents.Count(d =>
                    d.Status != BookingDocumentStatus.Verified && d.Status != BookingDocumentStatus.Waived),

                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);

        return PagedResult<BookingSummaryDto>.Ok(items, totalItems, pageSize);
    }
}
