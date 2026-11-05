using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a helper. Carries no <c>Code</c>: it is the handle other systems join on and it survives a
/// merge, so a rename would strand every legacy key pointing at this record.
/// </summary>
public class UpdateHelperCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public Address? Address { get; set; }
    public string? Notes { get; set; }

    /// <summary>Regular or on-call; null when not recorded.</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>The unit they normally work for (Cold Chain, wing van, trucking); null when not recorded.</summary>
    public CrewUnit? HomeUnit { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every load they were ever assigned to; it only stops the
    /// helper being offered for new work.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this helper and is deactivating it
    /// anyway. Requires <see cref="DependencyReason"/>. Ignored while the helper stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Helper);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
