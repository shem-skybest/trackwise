using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Creates a vessel. The first record a ship has ever had here - until now a vessel was whatever
/// somebody typed into <c>Workfile.VesselName</c>.
/// </summary>
public class CreateVesselCommand : ICommand<Result<VesselDto>>, IMasterDataWriteCommand
{
    /// <summary>
    /// Leave blank to have a code allocated from the <c>Vessel</c> number series. Not the IMO
    /// number: barges and domestic craft have none, and the code has to exist for every record.
    /// </summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>
    /// IMO number where the ship has one. Stored digits-only, so "IMO 9395044" and "9395044" are
    /// the same vessel to the duplicate probe.
    /// </summary>
    public string? ImoNumber { get; set; }

    public string? CallSign { get; set; }
    public string? FlagCountryCode { get; set; }
    public VesselType Type { get; set; }
    public int? TeuCapacity { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when every branch uses this vessel.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// ship. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Vessel);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,

        // Normalised before probing, not after: the stored column is digits-only, so probing with
        // the raw "IMO 9395044" a steward pastes in would match nothing at all.
        ["ImoNumber"] = Domain.Entities.Vessel.NormalizeImoNumber(ImoNumber),
        ["Name"] = Name
    };
}
