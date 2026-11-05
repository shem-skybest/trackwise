using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record NewLoadCreatedEvent(Guid LoadId) : IDomainEvent;
