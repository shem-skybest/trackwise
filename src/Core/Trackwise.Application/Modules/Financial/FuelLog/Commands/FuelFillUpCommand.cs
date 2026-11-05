using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Financial.FuelLog.Commands;

/// <summary>One fill-up as it's typed from the receipt.</summary>
public abstract class FuelFillUpCommand
{
    public Guid TruckId { get; set; }

    /// <summary>When the tank was filled, in UTC.</summary>
    public DateTime FilledAt { get; set; }

    public decimal Liters { get; set; }

    /// <summary>What was paid, VAT included.</summary>
    public decimal Amount { get; set; }

    /// <summary>Null when it wasn't charged to a fleet card.</summary>
    public FleetCard? FleetCard { get; set; }

    public int? OdometerReading { get; set; }
    public Guid? TripId { get; set; }
    public string? Station { get; set; }
    public string? Notes { get; set; }

    /// <summary>From <c>POST /expenses/receipt</c>. On a correction, leaving it empty keeps the receipt already attached.</summary>
    public string? ReceiptBlobPath { get; set; }
}
