using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>What each drop past the included ones costs on one truck size; a null size covers any.</summary>
public record RateExtraDropChargeDto
{
    public TruckTonnageClass? Tonnage { get; set; }
    public decimal Amount { get; set; }
}
