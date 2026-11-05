using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record NewTripCreatedEvent(Guid TripId) : IDomainEvent;
