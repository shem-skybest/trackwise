using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

/// <summary>A dispatched or in-transit trip was cancelled, so its driver had already been sent it.</summary>
public record TripCancelledEvent(Guid TripId, Guid? DriverId) : IDomainEvent;
