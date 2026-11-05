using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record TripCompletedEvent(Guid TripId) : IDomainEvent;
