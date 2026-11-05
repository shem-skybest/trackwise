using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.AddTariffRate;

public class AddTariffRateCommand : ICommand<Result<TariffRateDto>>
{
    public Guid TariffId { get; set; }
    public Guid ChargeCodeId { get; set; }
    public PricingModel PricingModel { get; set; }
    public decimal BaseAmount { get; set; }
    public string Unit { get; set; } = default!;
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public int? FreeDays { get; set; }
    public decimal? RateAfterFreeDays { get; set; }
    public decimal? Percentage { get; set; }
    public string? PercentageBase { get; set; }
    public string? BreakpointJson { get; set; }
}
