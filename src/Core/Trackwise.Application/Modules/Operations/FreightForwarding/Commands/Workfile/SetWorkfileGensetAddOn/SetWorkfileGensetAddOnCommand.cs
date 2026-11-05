using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Attaches or edits the genset add-on on a Reefer workfile - Genset is never booked standalone,
/// always an add-on to an existing file (see <see cref="Domain.Entities.WorkfileRules.SupportsGenset"/>).
/// </summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class SetWorkfileGensetAddOnCommand : ICommand<Result>
{
    public Guid Id { get; set; }

    /// <summary>The physical unit, if already assigned - assignment routinely lags the booking.</summary>
    public Guid? GensetId { get; set; }

    public GensetBillingType BillingType { get; set; }

    /// <summary>Required when <see cref="BillingType"/> is <see cref="GensetBillingType.Itemized"/>.</summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>Required when <see cref="BillingType"/> is <see cref="GensetBillingType.ProfitShare"/>.</summary>
    public decimal? FlatAmount { get; set; }

    public string? Notes { get; set; }
}
