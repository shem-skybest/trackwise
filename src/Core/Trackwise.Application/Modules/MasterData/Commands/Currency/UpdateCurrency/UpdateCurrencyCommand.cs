using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a currency. Carries no <c>Code</c>: it is the handle every <c>Money</c> in the schema
/// already holds as text, so a rename would strand every amount ever denominated in it.
/// </summary>
public class UpdateCurrencyCommand
    : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand, ICurrencyFields
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string? NumericCode { get; set; }
    public string? Symbol { get; set; }
    public int MinorUnits { get; set; } = 2;
    public string? Notes { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every amount denominated in it; it only stops the currency
    /// being offered for new work.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Accepted for consistency with every other retirement, but note that a currency's dependents
    /// are invisible to the probe - <c>Money.Currency</c> is text, not a foreign key - so this is
    /// never the thing standing between a steward and a retirement. See the policy table.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Currency);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
