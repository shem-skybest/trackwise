using Riok.Mapperly.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

/// <summary>
/// Entity -> DTO mapping for Booking. Mapperly generates only the large flat header mapping
/// (ToDtoCore); the small mappings are hand-written so a mismatch shows up as a plain compiler error.
/// </summary>
[Mapper]
public static partial class BookingMapper
{
    // ── Detail ───────────────────────────────────────────────────────────────

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapperIgnoreTarget(nameof(BookingDto.CustomerName))]
    [MapperIgnoreTarget(nameof(BookingDto.TotalDistanceKm))]
    [MapperIgnoreTarget(nameof(BookingDto.Stops))]
    [MapperIgnoreTarget(nameof(BookingDto.Cargo))]
    [MapperIgnoreTarget(nameof(BookingDto.Temperature))]
    [MapperIgnoreTarget(nameof(BookingDto.TotalSelling))]
    [MapperIgnoreTarget(nameof(BookingDto.TotalBuying))]
    [MapperIgnoreTarget(nameof(BookingDto.Charges))]
    [MapperIgnoreTarget(nameof(BookingDto.BillingInvoices))]
    [MapperIgnoreTarget(nameof(BookingDto.Documents))]
    [MapperIgnoreTarget(nameof(BookingDto.ConfirmationBlockers))]
    [MapperIgnoreTarget(nameof(BookingDto.Trips))]
    private static partial BookingDto ToDtoCore(this Booking entity);

    /// <summary>
    /// Full detail DTO. Requires Charges and Documents to be loaded on the entity. Everything that
    /// is not on the aggregate is passed in, batch-loaded by the caller, never read per row.
    /// </summary>
    /// <param name="locationNames">LocationId -> name for the lane's stops (one batched lookup).</param>
    public static BookingDto ToDto(
        this Booking entity,
        string customerName,
        IReadOnlyDictionary<Guid, string>? locationNames = null,
        List<BookingTripDto>? trips = null,
        List<BookingInvoiceSummaryDto>? billingInvoices = null)
    {
        var dto = entity.ToDtoCore();

        dto.CustomerName = customerName;

        // Money comes from the locked charge lines.
        var totals = entity.GetTotals();
        dto.TotalSelling = totals.Selling;
        dto.TotalBuying = totals.Buying;

        // Children
        dto.Charges = entity.Charges.Select(c => c.ToChargeDto()).ToList();
        dto.BillingInvoices = billingInvoices ?? [];
        dto.Documents = entity.Documents.Select(d => d.ToDocumentDto()).ToList();

        // Logistics
        dto.TotalDistanceKm = entity.Lane.TotalDistanceKm;
        dto.Stops = entity.Lane.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => s.ToStopDto(locationNames?.GetValueOrDefault(s.LocationId)))
            .ToList();

        if (entity.CargoSpec is { } cargo)
        {
            dto.Cargo = new BookingCargoDto
            {
                Commodity = cargo.Commodity,
                TotalWeightKg = cargo.TotalWeightKg,
                PalletCount = cargo.PalletCount,
                DeclaredValueAmount = cargo.DeclaredCargoValue.Amount,
                DeclaredValueCurrency = cargo.DeclaredCargoValue.Currency,
                RequiresActiveRefrigeration = cargo.RequiresActiveRefrigeration
            };
        }

        if (entity.TemperatureSpec is { } temp)
        {
            dto.Temperature = new BookingTemperatureDto
            {
                Zone = temp.Zone,
                SetpointCelsius = temp.SetpointCelsius,
                AllowableVarianceCelsius = temp.AllowableVarianceCelsius,
                Compliance = temp.Compliance,
                RequiresContinuousTelematics = temp.RequiresContinuousTelematics
            };
        }

        // Readiness for Confirm() so the UI can show what is still missing
        dto.ConfirmationBlockers = entity.GetConfirmationBlockers().ToList();

        dto.Trips = trips ?? [];
        return dto;
    }

    // ── Summary (for places that already hold a loaded entity) ───────────────
    // The list endpoint projects BookingSummaryDto straight from SQL (see GetBookingsHandler);
    // use this only after loading a Booking, e.g. to return the result of a command.
    // Requires Charges and Documents to be loaded on the entity.

    public static BookingSummaryDto ToSummaryDto(this Booking entity, string customerName) => new()
    {
        Id = entity.Id,
        BookingNumber = entity.BookingNumber,
        Status = entity.Status,
        CustomerId = entity.CustomerId,
        CustomerName = customerName,
        CustomerReference = entity.CustomerReference,
        QuotationId = entity.QuotationId,
        QuotationReference = entity.QuotationReference,
        Currency = entity.Currency,

        Mode = entity.Mode,
        FreightMode = entity.FreightMode,
        ShipmentType = entity.ShipmentType,
        RequestedPickupStart = entity.RequestedPickupStart,
        RequestedDeliveryEnd = entity.RequestedDeliveryEnd,

        TotalSelling = entity.Charges.Sum(c => c.SellingAmount),
        UnbilledSelling = entity.GetUnbilledCharges().Sum(c => c.SellingAmount),
        ChargeCount = entity.Charges.Count,

        CreditCheckStatus = entity.CreditCheckStatus,

        // Same rule as the SQL projection in GetBookingsHandler
        OpenDocumentCount = entity.Documents.Count(d =>
            d.Status != BookingDocumentStatus.Verified && d.Status != BookingDocumentStatus.Waived),

        CreatedAt = entity.CreatedAt
    };

    // ── Children ─────────────────────────────────────────────────────────────

    public static BookingChargeDto ToChargeDto(this BookingChargeLine entity) => new()
    {
        Id = entity.Id,
        Source = entity.Source,
        Category = entity.Category,
        ServiceType = entity.ServiceType,
        Description = entity.Description,
        Quantity = entity.Quantity,
        Currency = entity.Currency,
        SellingAmount = entity.SellingAmount,
        BuyingAmount = entity.BuyingAmount,
        IsBillable = entity.IsBillable,
        Reason = entity.Reason,
        BillingStatus = entity.BillingStatus,
        InvoiceId = entity.InvoiceId
    };

    public static BookingDocumentDto ToDocumentDto(this BookingDocument entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        Status = entity.Status,
        Reference = entity.Reference,
        ExpiresAt = entity.ExpiresAt,
        IsSatisfied = entity.IsSatisfied,
        ReviewedBy = entity.ReviewedBy,
        ReviewedAt = entity.ReviewedAt,
        ReviewNotes = entity.ReviewNotes
    };

    public static BookingStopDto ToStopDto(this RouteStop stop, string? locationName = null) => new()
    {
        Sequence = stop.Sequence,
        LocationId = stop.LocationId,
        LocationName = locationName,
        Type = stop.Type,
        WindowStart = stop.WindowStart,
        WindowEnd = stop.WindowEnd,
        Instructions = stop.Instructions
    };

    /// <summary>
    /// A planning-board row. Location names are batch-loaded by the caller (LocationId -> name),
    /// never read per row.
    /// </summary>
    public static UnAssignedBookingDto ToUnassignedDto(
        this Booking entity,
        string customerName,
        IReadOnlyDictionary<Guid, string> locationNames)
    {
        string NameOf(Guid locationId) => locationNames.GetValueOrDefault(locationId, "Unknown location");

        var stops = entity.Lane.Stops.OrderBy(s => s.Sequence).ToList();

        return new UnAssignedBookingDto
        {
            Id = entity.Id,
            BookingNumber = entity.BookingNumber,
            QuotationReference = entity.QuotationReference,
            CustomerId = entity.CustomerId,
            CustomerName = customerName,

            Mode = entity.Mode,
            FreightMode = entity.FreightMode,
            ShipmentType = entity.ShipmentType,

            RequestedPickupStart = entity.RequestedPickupStart,
            RequestedDeliveryEnd = entity.RequestedDeliveryEnd,

            OriginName = NameOf(entity.Lane.FirstPickup.LocationId),
            DestinationName = NameOf(entity.Lane.FinalDrop.LocationId),
            StopCount = stops.Count,
            DropNames = stops.Where(s => s.Type == StopType.Dropoff).Select(s => NameOf(s.LocationId)).ToList(),

            Commodity = entity.CargoSpec?.Commodity,
            TotalWeightKg = entity.CargoSpec?.TotalWeightKg,
            PalletCount = entity.CargoSpec?.PalletCount,

            IsTemperatureControlled = entity.IsTemperatureControlled,
            TemperatureZone = entity.TemperatureSpec?.Zone,
            SetpointCelsius = entity.TemperatureSpec?.SetpointCelsius,
            IsHazardous = entity.IsHazardous
        };
    }
}
