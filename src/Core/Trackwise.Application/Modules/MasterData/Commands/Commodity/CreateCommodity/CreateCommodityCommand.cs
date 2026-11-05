using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Creates a commodity. Like a vessel, the first record the thing has ever had here - until now a
/// commodity was whatever somebody typed into <c>Workfile.Commodity</c>.
/// </summary>
public class CreateCommodityCommand : ICommand<Result<CommodityDto>>, IMasterDataWriteCommand
{
    /// <summary>
    /// Leave blank to have a code allocated from the <c>Commodity</c> number series. Not the HS
    /// code: several commodities legitimately share one, so it cannot identify a record.
    /// </summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>Harmonized System tariff code. Stored digits-only, so "8471.30" and "847130" agree.</summary>
    public string? HsCode { get; set; }

    public string? Description { get; set; }
    public bool IsHazardous { get; set; }
    public string? UnNumber { get; set; }
    public bool RequiresTemperatureControl { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when every branch uses this commodity.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// commodity. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Commodity);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,
        ["Name"] = Name
    };
}
