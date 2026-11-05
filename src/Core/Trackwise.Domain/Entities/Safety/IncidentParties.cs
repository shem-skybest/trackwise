namespace Trackwise.Domain.Entities.Safety;

/// <summary>
/// Who and what an incident involved. Each is optional: a loading error at the client's warehouse
/// has no truck, a breakdown before loading has no trip.
/// </summary>
public sealed record IncidentParties(
    Guid? TripId,
    Guid? CustomerId,
    Guid? TruckId,
    Guid? DriverId,
    Guid? HelperId,
    Guid? SecondHelperId);
