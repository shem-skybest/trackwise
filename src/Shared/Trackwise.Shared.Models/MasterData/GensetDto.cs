using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>Read model for a genset - the canonical record behind what used to be no record at all.</summary>
public class GensetDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public GensetStatus Status { get; set; }

    /// <summary>Upper-trimmed. Null for units with none recorded.</summary>
    public string? SerialNumber { get; set; }

    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when the genset applies to every branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this genset into another. Non-null means the record is a
    /// tombstone that still resolves, not a live genset.
    /// </summary>
    public Guid? MergedIntoId { get; set; }

    /// <summary>Non-null for a soft-deleted record. Only ever populated on a deleted-records view.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set when the caller asked for an id that had been merged away and this record is the
    /// survivor it forwarded to. Lets a client say "that genset is now this one" rather than
    /// silently showing something the user did not ask for.
    /// </summary>
    public Guid? ForwardedFromId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
