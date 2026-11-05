using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record EmployeeDocumentDeletedEvent(Guid Id, Guid EmployeeId) : IDomainEvent;
