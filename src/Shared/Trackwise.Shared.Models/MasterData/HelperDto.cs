using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>Read model for a helper - the canonical record behind what used to be no record at all.</summary>
public class HelperDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? PhoneNumber { get; set; }
    public Address? Address { get; set; }
    public string? Notes { get; set; }

    /// <summary>Regular or on-call; null when not recorded.</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>The unit they normally work for; null when not recorded.</summary>
    public CrewUnit? HomeUnit { get; set; }

    /// <summary>Owning branch, or null when the helper applies to every branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this helper into another. Non-null means the record is a
    /// tombstone that still resolves, not a live helper.
    /// </summary>
    public Guid? MergedIntoId { get; set; }

    /// <summary>Non-null for a soft-deleted record. Only ever populated on a deleted-records view.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set when the caller asked for an id that had been merged away and this record is the
    /// survivor it forwarded to. Lets a client say "that helper is now this one" rather than
    /// silently showing something the user did not ask for.
    /// </summary>
    public Guid? ForwardedFromId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
