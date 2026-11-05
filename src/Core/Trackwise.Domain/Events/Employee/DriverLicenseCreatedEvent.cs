using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

public record DriverLicenseCreatedEvent(Guid LicenseId, Guid EmployeeId) : IDomainEvent;
