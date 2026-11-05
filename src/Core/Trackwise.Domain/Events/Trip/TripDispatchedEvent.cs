using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record TripDispatchedEvent(Guid TripId) : IDomainEvent;
