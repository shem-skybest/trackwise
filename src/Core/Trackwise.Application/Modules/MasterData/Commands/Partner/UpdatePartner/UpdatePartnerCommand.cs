using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a business partner. Carries no <c>Code</c>: it is the handle the accounting database and
/// nSFS join on and it survives a merge, so a rename would strand every legacy key pointing here.
/// <para>
/// ⚠️ <b>What this command deliberately cannot do is publish.</b> <see cref="IsActive"/> may be
/// cleared, and it may be set again on a partner that has already been through approval - but it
/// can never take a draft live, because that is <see cref="ApprovePartnerCommand"/>'s job and a
/// second door to it would be a way around the second pair of eyes.
/// </para>
/// <para>
/// ⚠️ Editing an <i>approved</i> partner takes effect immediately; the edit is not itself
/// re-approved. Holding an edit for review needs somewhere to keep the proposed values while the
/// live record keeps trading, and the module has no such shadow - see MASTER_DATA_PLAN.md §7. What
/// is gated today is a partner becoming usable at all, which is what the definition of done asks.
/// </para>
/// </summary>
public class UpdatePartnerCommand
    : ICommand<Result>, IHaveId, IMasterDataWriteCommand, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public List<BusinessPartnerRole> Roles { get; set; } = [];
    public Address? Address { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the partner is offered for new work. Refused on a record that has not been approved:
    /// a draft is inactive by definition, and the way out of that is approval, not an edit.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// company. Requires <see cref="DistinctReason"/> - an edit can create a duplicate just as a
    /// create can, by renaming one partner into another one's name.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    /// <summary>
    /// Set after the steward has seen what still names this partner and is deactivating it anyway.
    /// Requires <see cref="DependencyReason"/>. Ignored while the partner stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.BusinessPartner);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => Id;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        // No Code: the command cannot change it, so probing on it could only ever match the record
        // against itself, which the probe already excludes.
        ["TaxId"] = Domain.Entities.BusinessPartner.NormalizeTaxId(TaxId),
        ["Name"] = Name
    };

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.BusinessPartner);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    /// <summary>
    /// Every edit to a draft reports a deactivation here, because a draft is never active and the
    /// command cannot see the record's current state. That costs nothing: the dependency probe's
    /// SQL hangs its counts off the record's own row with <c>AND is_active</c>, so a record that is
    /// already inactive comes back with nothing to report and the write proceeds.
    /// </summary>
    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
