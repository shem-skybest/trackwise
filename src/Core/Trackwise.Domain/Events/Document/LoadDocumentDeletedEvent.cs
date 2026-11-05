using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record LoadDocumentDeletedEvent(Guid DocumentId, Guid LoadId) : IDomainEvent;
