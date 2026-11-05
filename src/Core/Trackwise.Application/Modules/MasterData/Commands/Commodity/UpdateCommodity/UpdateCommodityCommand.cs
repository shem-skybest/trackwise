using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a commodity. Carries no <c>Code</c>: it is the handle other systems join on and it
/// survives a merge, so a rename would strand every legacy key pointing at this record.
/// </summary>
public class UpdateCommodityCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string? HsCode { get; set; }
    public string? Description { get; set; }
    public bool IsHazardous { get; set; }
    public string? UnNumber { get; set; }
    public bool RequiresTemperatureControl { get; set; }
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every workfile that references it; it only stops the
    /// commodity being offered for new work.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this commodity and is deactivating it
    /// anyway. Requires <see cref="DependencyReason"/>. Ignored while the commodity stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Commodity);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
