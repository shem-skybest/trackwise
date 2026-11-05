using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Creates a port. Writes the same <c>terminals</c> record dispatch reads - a port and a terminal
/// are one thing under two names, and keeping them one record is the point of the module.
/// </summary>
public class CreatePortCommand : ICommand<Result<TerminalDto>>, IMasterDataWriteCommand
{
    /// <summary>
    /// UN/LOCODE where the port has one. Leave blank to have a code allocated from the
    /// <c>Terminal</c> number series - inland depots and border crossings often have none.
    /// </summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public TerminalType Type { get; set; }
    public Address Address { get; set; } = default!;
    public string? Notes { get; set; }

    /// <summary>
    /// Owning branch, or null when every branch uses this port. Scoping governs who maintains the
    /// record, not who can read it.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// port. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Terminal);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,
        ["Name"] = Name
    };
}
