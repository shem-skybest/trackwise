using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Soft-deletes a port. The row survives so existing loads and containers keep resolving; it simply
/// stops appearing in ordinary queries. Reversed by <see cref="RestorePortCommand" />.
/// </summary>
public class DeletePortCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    /// <summary>
    /// Set after the steward has seen what still points at this port and is deleting it anyway.
    /// Requires <see cref="DependencyReason"/>.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Terminal);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation => MasterDataDeactivation.Delete;
}
