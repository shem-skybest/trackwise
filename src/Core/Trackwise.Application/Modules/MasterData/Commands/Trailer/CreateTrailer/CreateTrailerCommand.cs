using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Creates a trailer. The first record a trailer has ever had in this domain.</summary>
public class CreateTrailerCommand : ICommand<Result<TrailerDto>>, IMasterDataWriteCommand
{
    /// <summary>Leave blank to have a code allocated from the <c>Trailer</c> number series.</summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;
    public TrailerType Type { get; set; }

    /// <summary>17-character VIN, where one has been recorded. Stored upper-trimmed.</summary>
    public string? Vin { get; set; }

    public string? LicensePlate { get; set; }
    public string? LicensePlateState { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public int? AxleCount { get; set; }
    public decimal? MaxLoadCapacityKg { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when every branch uses this trailer.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// trailer. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Trailer);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,
        ["Vin"] = Domain.Entities.Trailer.NormalizeVin(Vin),
        ["Name"] = Name
    };
}
