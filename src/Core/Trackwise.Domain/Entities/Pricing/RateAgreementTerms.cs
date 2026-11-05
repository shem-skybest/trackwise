using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The conditions a Cold Chain quotation sets under its rate table: how fuel is added, how many drops
/// the rate covers and what each further drop costs, what a foul trip or a backload is charged,
/// waiting time, and what is passed on at cost. A null term means the quotation doesn't say - not
/// that it is free.
/// </summary>
/// <param name="IncludedDrops">Drops the trip rate covers ("maximum of five (5) drops"); null means one.</param>
/// <param name="FoulTripPercent">Share of the trip rate charged when nothing could be delivered, e.g. 50.</param>
/// <param name="BackloadPercent">Share of the trip rate for bringing goods back to the warehouse.</param>
/// <param name="DemurrageFreeHours">Waiting time at the warehouse or an outlet before demurrage starts.</param>
/// <param name="DemurrageHourlyRate">Charged per hour of waiting past the free hours.</param>
/// <param name="OtherLocationCharge">Added for a drop outside the trip's delivery area ("different delivery location").</param>
/// <param name="TollsAndParkingAtCost">True when tolls and parking are billed on receipts; false when the rate includes them.</param>
/// <param name="BillExtraDrops">
/// False when drops past <paramref name="IncludedDrops"/> are counted but not charged. Skybest's quotations
/// list an extra-drop fee, yet SAS billed none of the 37 trips past their included drops in Jul-Sep 2026.
/// </param>
/// <param name="RatesIncludeVat">
/// True when the card's amounts include 12% VAT (Pacific's and HFI's). The trip is then billed net, as
/// nSFS records it, so the billing batch's VAT isn't added twice.
/// </param>
public sealed record RateAgreementTerms(
    FuelAdjustmentMethod FuelMethod,
    int? IncludedDrops = null,
    IReadOnlyList<(TruckTonnageClass? Tonnage, decimal Amount)>? ExtraDropCharges = null,
    decimal? FoulTripPercent = null,
    decimal? BackloadPercent = null,
    decimal? DemurrageFreeHours = null,
    decimal? DemurrageHourlyRate = null,
    decimal? OtherLocationCharge = null,
    bool? TollsAndParkingAtCost = null,
    bool BillExtraDrops = true,
    bool RatesIncludeVat = false);
