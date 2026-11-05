using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record EmployeeDocumentUploadedEvent(Guid DocId, Guid EmployeeId) : IDomainEvent;
