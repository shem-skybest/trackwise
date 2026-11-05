using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>
/// Read model for a trading counterparty on a freight-forwarding file, and for the master-data
/// record behind it.
/// <para>
/// One DTO rather than a second master-data shape beside it: a parallel <c>PartnerDto</c> would
/// recreate on the read side exactly the duplication the module removed on the write side, and the
/// two would start disagreeing about what a partner is.
/// </para>
/// </summary>
public class BusinessPartnerDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required List<BusinessPartnerRole> Roles { get; set; }
    public Address? Address { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>VAT ID / TIN, digits only - the form the duplicate probe matches on.</summary>
    public string? TaxId { get; set; }

    public Guid? CustomerId { get; set; }

    /// <summary>Owning branch, or null when every branch trades with this partner.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Where the record sits in maker-checker. The one stored part of <see cref="State"/>.
    /// </summary>
    public MasterDataApprovalState ApprovalState { get; set; }

    /// <summary>
    /// The record's lifecycle state, derived from its flags rather than stored, so a client renders
    /// one badge instead of reassembling the precedence between active, deleted and merged - and
    /// getting it wrong the same way every caller does.
    /// </summary>
    public MasterDataState State { get; set; }

    /// <summary>
    /// Who is waiting on whom, for a record that is <see cref="MasterDataApprovalState.Pending"/>.
    /// Null otherwise. Lets the steward's own screen say "with an approver since Tuesday" without a
    /// second request.
    /// </summary>
    public string? SubmittedByName { get; set; }

    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// The last reviewer's note, which on a record back in draft is the reason it came back. The
    /// one thing a steward needs and cannot get from the record itself.
    /// </summary>
    public string? LastReviewNote { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Set when a steward merged this partner into another. Non-null means the record is a
    /// tombstone that still resolves, not a live partner.
    /// </summary>
    public Guid? MergedIntoId { get; set; }

    /// <summary>Non-null for a soft-deleted record. Only ever populated on a deleted-records view.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set when the caller asked for an id that had been merged away and this record is the
    /// survivor it forwarded to. Lets a client say "that partner is now this one" rather than
    /// silently showing something the user did not ask for.
    /// </summary>
    public Guid? ForwardedFromId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
