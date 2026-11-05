using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Creates a genset unit. The first record a genset has ever had in this domain.</summary>
public class CreateGensetCommand : ICommand<Result<GensetDto>>, IMasterDataWriteCommand
{
    /// <summary>Leave blank to have a code allocated from the <c>Genset</c> number series.</summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;
    public GensetStatus Status { get; set; }

    /// <summary>Manufacturer serial number, where one has been recorded. Stored upper-trimmed.</summary>
    public string? SerialNumber { get; set; }

    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when every branch uses this genset.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// genset. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Genset);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,
        ["SerialNumber"] = Domain.Entities.Genset.NormalizeSerialNumber(SerialNumber),
        ["Name"] = Name
    };
}
