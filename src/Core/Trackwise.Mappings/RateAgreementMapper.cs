using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class RateAgreementMapper
{
    /// <summary>Header only - list reads never load lines.</summary>
    [MapperIgnoreSource(nameof(RateAgreement.DomainEvents))]
    [MapperIgnoreSource(nameof(RateAgreement.Customer))]
    [MapperIgnoreSource(nameof(RateAgreement.Lines))]
    [MapperIgnoreSource(nameof(RateAgreement.ExtraDropCharges))]
    [MapperIgnoreSource(nameof(RateAgreement.Terms))]
    [MapperIgnoreSource(nameof(RateAgreement.UpdatedBy))]
    [MapperIgnoreTarget(nameof(RateAgreementDto.ExtraDropCharges))]
    [MapperIgnoreTarget(nameof(RateAgreementDto.CustomerName))]
    [MapperIgnoreTarget(nameof(RateAgreementDto.LineCount))]
    [MapperIgnoreTarget(nameof(RateAgreementDto.Lines))]
    public static partial RateAgreementDto ToHeaderDto(this RateAgreement entity);

    /// <summary>
    /// With its lines. Surcharges come from a lookup the caller batched: reading each line's
    /// navigation would be a query per line, and Bayview's card alone has 93.
    /// </summary>
    public static RateAgreementDto ToDto(
        this RateAgreement entity, string? customerName, ILookup<Guid, RateFuelSurcharge> surcharges)
    {
        var lines = entity.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => l.ToDto() with
            {
                FuelSurcharges = surcharges[l.Id].OrderBy(s => s.DieselPrice).Select(s => s.ToDto()).ToList()
            })
            .ToList();

        var extraDrops = entity.ExtraDropCharges
            .OrderBy(c => c.Tonnage)
            .Select(c => new RateExtraDropChargeDto { Tonnage = c.Tonnage, Amount = c.Amount })
            .ToList();

        return entity.ToHeaderDto() with
        {
            CustomerName = customerName,
            Lines = lines,
            LineCount = lines.Count,
            ExtraDropCharges = extraDrops
        };
    }

    [MapperIgnoreSource(nameof(RateAgreementLine.DomainEvents))]
    [MapperIgnoreSource(nameof(RateAgreementLine.RateAgreementId))]
    [MapperIgnoreSource(nameof(RateAgreementLine.SortOrder))]
    [MapperIgnoreSource(nameof(RateAgreementLine.IsDistanceBand))]
    [MapperIgnoreSource(nameof(RateAgreementLine.FuelSurcharges))]
    [MapperIgnoreTarget(nameof(RateAgreementLineDto.FuelSurcharges))]
    public static partial RateAgreementLineDto ToDto(this RateAgreementLine entity);

    [MapperIgnoreSource(nameof(RateFuelSurcharge.DomainEvents))]
    [MapperIgnoreSource(nameof(RateFuelSurcharge.Id))]
    [MapperIgnoreSource(nameof(RateFuelSurcharge.RateAgreementLineId))]
    public static partial RateFuelSurchargeDto ToDto(this RateFuelSurcharge entity);

    [MapperIgnoreSource(nameof(DieselPrice.DomainEvents))]
    [MapperIgnoreSource(nameof(DieselPrice.UpdatedAt))]
    [MapperIgnoreSource(nameof(DieselPrice.UpdatedBy))]
    public static partial DieselPriceDto ToDto(this DieselPrice entity);

    /// <summary>The command/DTO line shape into the domain's line spec.</summary>
    public static RateLineSpec ToSpec(this RateAgreementLineDto line) => new(
        line.Label,
        line.Tonnage,
        line.Amount,
        line.MinKm,
        line.MaxKm,
        line.City,
        line.Province,
        line.DistanceKm,
        line.FuelPercent,
        line.FuelSurcharges.Select(s => (s.DieselPrice, s.Amount)).ToList());
}
