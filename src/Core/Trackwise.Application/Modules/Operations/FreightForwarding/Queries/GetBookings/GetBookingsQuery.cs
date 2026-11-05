using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

public class GetBookingsQuery : IQuery<PagedResult<BookingSummaryDto>>
{
    public Guid? CustomerId { get; set; }
    public BookingStatus? Status { get; set; }
    public TransportMode? Mode { get; set; }
    public FreightMode? FreightMode { get; set; }
    public CreditCheckStatus? CreditCheckStatus { get; set; }

    /// <summary>Booking number, quotation reference or customer reference (contains, case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>True: only bookings with charges not yet on an invoice. Handy for the billing queue.</summary>
    public bool? HasUnbilledCharges { get; set; }

    /// <summary>Filters on the requested pickup start.</summary>
    public DateTimeOffset? PickupFrom { get; set; }
    public DateTimeOffset? PickupTo { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
