using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

public class UpdateBranchCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string? ShortName { get; set; }
    public Address? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxId { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every reference to it; it only stops the branch being
    /// offered for new work. Use delete to remove it from ordinary views.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this branch and is deactivating it
    /// anyway. Requires <see cref="DependencyReason"/>. Ignored when the branch stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Branch);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    /// <summary>
    /// An edit that leaves the branch active costs nothing; only clearing the flag is a retirement.
    /// The probe itself confirms this is a real transition, so an update that merely restates
    /// <c>IsActive = false</c> on an already-inactive branch is not refused.
    /// </summary>
    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
