using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>
/// Records the diesel price from a date. Setting a date that already has one corrects it, so a typo
/// is fixed in place rather than stacked.
/// </summary>
public class SetDieselPriceCommand : ICommand<Result<DieselPriceDto>>
{
    public DateOnly EffectiveFrom { get; set; }
    public decimal PricePerLitre { get; set; }
    public string? Notes { get; set; }
}
