using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// One rate card row, read and written in the same shape: a place (city and/or province) or a
/// distance band (min/max km), for one truck size or any.
/// </summary>
public record RateAgreementLineDto
{
    public Guid? Id { get; set; }
    public required string Label { get; set; }
    public TruckTonnageClass? Tonnage { get; set; }
    public decimal Amount { get; set; }
    public int? MinKm { get; set; }
    public int? MaxKm { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public double? DistanceKm { get; set; }
    public decimal? FuelPercent { get; set; }
    public IReadOnlyList<RateFuelSurchargeDto> FuelSurcharges { get; set; } = [];
}
