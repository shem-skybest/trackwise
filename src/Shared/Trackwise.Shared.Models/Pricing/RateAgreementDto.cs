using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>A client's truck-trip rate agreement. <see cref="Lines"/> is empty on list reads.</summary>
public record RateAgreementDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? CustomerName { get; set; }

    public required string Name { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public RateAgreementStatus Status { get; set; }
    public FuelAdjustmentMethod FuelMethod { get; set; }
    public int? IncludedDrops { get; set; }

    /// <summary>Empty on list reads, like <see cref="Lines"/>.</summary>
    public IReadOnlyList<RateExtraDropChargeDto> ExtraDropCharges { get; set; } = [];

    public decimal? FoulTripPercent { get; set; }
    public decimal? BackloadPercent { get; set; }
    public decimal? DemurrageFreeHours { get; set; }
    public decimal? DemurrageHourlyRate { get; set; }
    public decimal? OtherLocationCharge { get; set; }
    public bool? TollsAndParkingAtCost { get; set; }
    public bool BillExtraDrops { get; set; }
    public bool RatesIncludeVat { get; set; }
    public required string Currency { get; set; }
    public string? Notes { get; set; }
    public Guid? SupersedesId { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int LineCount { get; set; }
    public IReadOnlyList<RateAgreementLineDto> Lines { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
