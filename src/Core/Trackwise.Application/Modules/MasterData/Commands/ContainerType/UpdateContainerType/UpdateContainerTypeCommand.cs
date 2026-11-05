using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a container type. Carries no <c>Code</c>: it is the handle other systems join on and it
/// survives a merge, so a rename would strand every legacy key pointing at this record.
/// </summary>
public class UpdateContainerTypeCommand
    : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand, IContainerTypeFields
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string? IsoSizeTypeCode { get; set; }
    public ContainerTypeCategory Category { get; set; }
    public int? LengthFeet { get; set; }
    public bool IsHighCube { get; set; }
    public decimal? TeuFactor { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every container, booking and tariff that references it;
    /// it only stops the type being offered for new work.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this type and is deactivating it
    /// anyway. Requires <see cref="DependencyReason"/>. Ignored while the type stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.ContainerType);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
