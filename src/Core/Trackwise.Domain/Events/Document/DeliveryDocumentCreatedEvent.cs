using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Events;

public record DeliveryDocumentCreatedEvent(Guid DocumentId, Guid LoadId, DocumentType DocumentType) : IDomainEvent;
