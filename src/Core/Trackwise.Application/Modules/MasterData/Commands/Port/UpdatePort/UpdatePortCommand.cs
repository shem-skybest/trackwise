using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a port. Carries no <c>Code</c>: it is the handle other systems join on and it survives a
/// merge, so a rename would strand every legacy key pointing at this record.
/// </summary>
public class UpdatePortCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public TerminalType Type { get; set; }
    public Address Address { get; set; } = default!;
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every load and container that references it; it only stops
    /// the port being offered for new work. Use delete to remove it from ordinary views.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this port and is deactivating it anyway.
    /// Requires <see cref="DependencyReason"/>. Ignored when the port stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Terminal);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    /// <summary>
    /// An edit that leaves the port active costs nothing; only clearing the flag is a retirement.
    /// The probe itself confirms this is a real transition, so an update that merely restates
    /// <c>IsActive = false</c> on an already-inactive port is not refused.
    /// </summary>
    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
