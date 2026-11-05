using Trackwise.Domain.Core;

namespace Trackwise.Domain.Events;

/// <summary>
/// Raised by the expiry-reminder job when a license crosses an expiry threshold (60/30/7 days).
/// </summary>
public record DriverLicenseExpiringEvent(
    Guid LicenseId,
    Guid EmployeeId,
    DateTime ExpiresAt,
    int DaysUntilExpiry) : IDomainEvent;
