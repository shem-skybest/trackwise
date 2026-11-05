using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>One fill-up as its receipt shows it - one row of Monday's Fleet Gas Expenses board.</summary>
/// <param name="FilledAt">When the tank was filled, in UTC.</param>
/// <param name="Amount">What was paid, VAT included, as the pump receipt shows it.</param>
/// <param name="FleetCard">The card it was charged to; null when it was paid some other way.</param>
/// <param name="TripId">The trip the fuel was for, when Ops knows it.</param>
/// <param name="Station">The station and where it is, e.g. "Shell Molino, Bacoor".</param>
public sealed record FuelFillUp(
    Guid TruckId,
    DateTime FilledAt,
    decimal Liters,
    Money Amount,
    FleetCard? FleetCard,
    int? OdometerReading,
    Guid? TripId,
    string? Station,
    string? Notes);
