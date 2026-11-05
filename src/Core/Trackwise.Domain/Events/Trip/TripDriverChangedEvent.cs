using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

/// <summary>The driver of a trip already on the driver's phone changed.</summary>
public record TripDriverChangedEvent(Guid TripId, Guid? OldDriverId) : IDomainEvent;
