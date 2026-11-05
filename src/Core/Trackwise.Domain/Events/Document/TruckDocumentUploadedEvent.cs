using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record TruckDocumentUploadedEvent(Guid DocId, Guid TruckId) : IDomainEvent;
