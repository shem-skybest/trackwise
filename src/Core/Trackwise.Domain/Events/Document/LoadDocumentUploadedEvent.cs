using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record LoadDocumentUploadedEvent(Guid DocumentId, Guid LoadId) : IDomainEvent;
