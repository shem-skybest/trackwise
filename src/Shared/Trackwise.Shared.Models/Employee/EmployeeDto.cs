using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public class EmployeeDto
{
    public Guid? Id { get; set; }
    public string? Code { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>Display label only; permissions come from <see cref="Role" />.</summary>
    public string? JobTitle { get; set; }
    public string? FullName { get; set; }
    public Address? Address { get; set; }
    public string? LastKnownLocation { get; set; }
    public double? LastKnownLocationLat { get; set; }
    public double? LastKnownLocationLng { get; set; }
    public decimal Salary { get; set; }
    public SalaryType SalaryType { get; set; }
    public EmployeeStatus Status { get; set; }
    public DateTime JoinedDate { get; set; }
    public string? StripeConnectedAccountId { get; set; }
    public RoleDto? Role { get; set; }

    /// <summary>For drivers: regular or on-call. Null for office staff, or when not recorded.</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>For drivers: the unit they normally work for. Null for office staff, or when not recorded.</summary>
    public CrewUnit? HomeUnit { get; set; }

    /// <summary>Owning branch, or null when the employee applies to every branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this employee into another. Non-null means the record is a
    /// tombstone that still resolves, not a live employee.
    /// </summary>
    public Guid? MergedIntoId { get; set; }

    /// <summary>Non-null for a soft-deleted record. Only ever populated on a deleted-records view.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set when the caller asked for an id that had been merged away and this record is the
    /// survivor it forwarded to.
    /// </summary>
    public Guid? ForwardedFromId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
