using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>One fill-up in the fuel log. Truck and trip numbers are resolved by the handler in batched lookups.</summary>
public record FuelFillUpDto
{
    public Guid Id { get; set; }
    public long Number { get; set; }
    public Guid TruckId { get; set; }
    public string? TruckNumber { get; set; }
    public Guid? TripId { get; set; }
    public long? TripNumber { get; set; }
    public DateTime FilledAt { get; set; }
    public decimal Liters { get; set; }
    public decimal PricePerLiter { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public FleetCard? FleetCard { get; set; }
    public int? OdometerReading { get; set; }

    /// <summary>Kilometres since the truck's previous odometer reading, when both are known.</summary>
    public int? KmSincePrevious { get; set; }

    /// <summary>Those kilometres over every liter put in since that reading, this fill-up's included.</summary>
    public decimal? KmPerLiter { get; set; }

    public string? Station { get; set; }
    public string? Notes { get; set; }
    public bool HasReceipt { get; set; }

    /// <summary>Pending is "for review", Approved is "verified", Rejected is "flagged".</summary>
    public ExpenseStatus Status { get; set; }

    public string? FlaggedReason { get; set; }
}
