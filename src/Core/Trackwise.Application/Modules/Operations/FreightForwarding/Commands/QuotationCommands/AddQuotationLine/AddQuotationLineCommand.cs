using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AddQuotationLine;

public class AddQuotationLineCommand : ICommand<Result<Guid>>
{
    public Guid QuotationId { get; set; }
    public Guid TariffRateId { get; set; }

    /// <summary>
    /// Operational service classification (LineHaul, PreCooling, StopOff, Telematics, etc.).
    /// Defaults to Standard if not specified.
    /// </summary>
    public OperationalServiceType ServiceType { get; set; } = OperationalServiceType.Standard;

    // REMOVED: ChargeCategory? Category.
    // The category is now persisted on the line and drives ValidateOperationalIntegrity ("must have a Base
    // charge") and discount sign handling. Letting callers override it would let an Accessorial line be
    // relabelled as Base to dodge that check. The category always comes from the rate's ChargeCode.

    public decimal Quantity { get; set; }
    public MarkupType MarkupType { get; set; }
    public decimal MarkupValue { get; set; }

    /// <summary>Multiplier from the charge currency to the quotation currency. Only valid when they differ.</summary>
    public decimal? FxRate { get; set; }

    /// <summary>
    /// Explicit valuation base for Percentage rates (in the quotation currency). Needed when the rate's
    /// PercentageBase is not "BAS" or "CIF" (those are derived from the quotation).
    /// </summary>
    public decimal? BaseAmount { get; set; }

    public string? Notes { get; set; }
}
