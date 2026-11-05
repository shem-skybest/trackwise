using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>
/// Read model for a terminal, which is also the master-data record for a port. Dispatch reads the
/// operational half; the master-data screens read the stewardship half below it.
/// </summary>
public class TerminalDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string CountryCode { get; set; }
    public TerminalType Type { get; set; }
    public required Address Address { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when the port applies to every branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this port into another. Non-null means the record is a tombstone
    /// that still resolves, not a live port.
    /// </summary>
    public Guid? MergedIntoId { get; set; }

    /// <summary>Non-null for a soft-deleted record. Only ever populated on a deleted-records view.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set when the caller asked for an id that had been merged away and this record is the
    /// survivor it forwarded to. Lets a client say "that port is now this one" rather than silently
    /// showing something the user did not ask for.
    /// </summary>
    public Guid? ForwardedFromId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
