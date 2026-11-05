using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Creates a currency.</summary>
public class CreateCurrencyCommand : ICommand<Result<CurrencyDto>>, IMasterDataWriteCommand, ICurrencyFields
{
    /// <summary>
    /// ISO 4217 alpha-3, and required - unlike every other master code in the module, this one is
    /// never allocated from the number series. A standards body assigns it, so a blank here is an
    /// omission rather than a request for one.
    /// </summary>
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;

    /// <summary>ISO 4217 numeric code, three digits.</summary>
    public string? NumericCode { get; set; }

    public string? Symbol { get; set; }

    /// <summary>Decimal places the currency subdivides into. Two unless the caller says otherwise.</summary>
    public int MinorUnits { get; set; } = 2;

    public string? Notes { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// currency. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Currency);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,
        ["NumericCode"] = NumericCode,
        ["Name"] = Name
    };
}
