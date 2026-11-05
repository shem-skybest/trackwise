using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a vessel. Carries no <c>Code</c>: it is the handle other systems join on and it survives
/// a merge, so a rename would strand every legacy key pointing at this record.
/// <para>
/// It does carry <c>ImoNumber</c>, which is the opposite case - a ship whose IMO was never recorded
/// gets it added later, and a number identifying the same hull for life is exactly the field worth
/// correcting.
/// </para>
/// </summary>
public class UpdateVesselCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string? ImoNumber { get; set; }
    public string? CallSign { get; set; }
    public string? FlagCountryCode { get; set; }
    public VesselType Type { get; set; }
    public int? TeuCapacity { get; set; }
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every workfile that references it; it only stops the
    /// vessel being offered for new work.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this vessel and is deactivating it
    /// anyway. Requires <see cref="DependencyReason"/>. Ignored while the vessel stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Vessel);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
