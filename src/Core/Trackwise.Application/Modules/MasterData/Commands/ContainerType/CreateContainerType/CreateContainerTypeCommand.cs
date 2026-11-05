using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Creates a container or equipment type.</summary>
public class CreateContainerTypeCommand : ICommand<Result<ContainerTypeDto>>, IMasterDataWriteCommand, IContainerTypeFields
{
    /// <summary>
    /// Leave blank to have a code allocated from the <c>ContainerType</c> number series. In
    /// practice a steward types the yard shorthand - <c>HC40</c> - because that is the name the
    /// equipment already has.
    /// </summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>ISO 6346 size-type code, four characters. Blank for non-ISO equipment.</summary>
    public string? IsoSizeTypeCode { get; set; }

    public ContainerTypeCategory Category { get; set; }
    public int? LengthFeet { get; set; }
    public bool IsHighCube { get; set; }
    public decimal? TeuFactor { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// type. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.ContainerType);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,
        ["IsoSizeTypeCode"] = IsoSizeTypeCode,
        ["Name"] = Name
    };
}
