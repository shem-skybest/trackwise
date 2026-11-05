using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Opens a workfile. Normally a promotion from a Confirmed booking; Reefer files carry no booking
/// at all, so <see cref="BookingId"/> is optional and <see cref="CustomerId"/> stands in.
/// </summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class PromoteBookingToWorkfileCommand : ICommand<Result<Guid>>
{
    public Guid? BookingId { get; set; }

    /// <summary>Required only when there is no booking to take the customer from.</summary>
    public Guid? CustomerId { get; set; }

    public WorkfileType Type { get; set; }
    public FreightMode? Direction { get; set; }
    public TruckingVariant? Variant { get; set; }

    /// <summary>
    /// Nullable so an unset value is rejected rather than defaulting to PSB - the legacy screen
    /// forced this choice before the first save.
    /// </summary>
    public ReportCategory? ReportCategory { get; set; }

    /// <summary>Overrides the allocated reference; must still be unique.</summary>
    public string? ReferenceNumber { get; set; }

    public string? BillOfLadingNumber { get; set; }
    public BillIndicator? BillIndicator { get; set; }
    public string? VesselName { get; set; }
    public string? VoyageNumber { get; set; }
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public string? Commodity { get; set; }
    public Guid? ShippingLineId { get; set; }
    public Guid? CoLoaderId { get; set; }
    public Guid? ForwarderId { get; set; }
    public Guid? TruckerId { get; set; }
    public Guid? BrokerId { get; set; }
    public int? CreditTermDays { get; set; }
    public CreditType? CreditType { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? TaggingCategory { get; set; }
    public bool IsAdvanceBilling { get; set; }
}
