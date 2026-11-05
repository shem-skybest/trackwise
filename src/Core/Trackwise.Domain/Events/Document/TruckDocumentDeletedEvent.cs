using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record TruckDocumentDeletedEvent(Guid Id, Guid TruckId) : IDomainEvent;
