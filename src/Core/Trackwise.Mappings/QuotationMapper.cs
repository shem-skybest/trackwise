using Riok.Mapperly.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Quotations;

namespace Trackwise.Mappings;

[Mapper]
public static partial class QuotationMapper
{
    // =========================================================================
    // 1. Quotation -> QuotationDto
    // =========================================================================

    [MapperIgnoreSource(nameof(Quotation.DomainEvents))]
    [MapperIgnoreSource(nameof(Quotation.Lines))]
    // Ignore Lane & CargoSpec objects because we flatten their child properties below
    [MapperIgnoreSource(nameof(Quotation.Lane))]
    [MapperIgnoreSource(nameof(Quotation.CargoSpec))]
    // Target fields resolved asynchronously or computed via overloads
    [MapperIgnoreTarget(nameof(QuotationDto.CustomerName))]
    [MapperIgnoreTarget(nameof(QuotationDto.BranchName))]
    [MapperIgnoreTarget(nameof(QuotationDto.Stops))]
    [MapperIgnoreTarget(nameof(QuotationDto.Lines))]
    [MapperIgnoreTarget(nameof(QuotationDto.TotalBuyingAmount))]
    [MapperIgnoreTarget(nameof(QuotationDto.TotalSellingAmount))]
    [MapperIgnoreTarget(nameof(QuotationDto.Revisions))]
    // Flatten Lane properties
    [MapProperty("Lane.TotalDistanceKm", nameof(QuotationDto.TotalDistanceKm))]
    // Flatten CargoSpec properties
    [MapProperty("CargoSpec.Commodity", nameof(QuotationDto.Commodity))]
    [MapProperty("CargoSpec.TotalWeightKg", nameof(QuotationDto.TotalWeightKg))]
    [MapProperty("CargoSpec.PalletCount", nameof(QuotationDto.PalletCount))]
    [MapProperty("CargoSpec.DeclaredCargoValue.Amount", nameof(QuotationDto.DeclaredCargoValueAmount))]
    public static partial QuotationDto ToDto(this Quotation entity);

    /// <summary>
    /// Nested Value Object Mapper: Automatically utilized by Mapperly 
    /// when mapping Quotation.TemperatureSpec to QuotationDto.TemperatureSpec.
    /// </summary>
    public static partial TemperatureControlDto ToDto(this TemperatureControl entity);

    // =========================================================================
    // 2. Quotation -> QuotationSummaryDto
    // =========================================================================

    [MapperIgnoreSource(nameof(Quotation.DomainEvents))]
    [MapperIgnoreSource(nameof(Quotation.Lines))]
    [MapperIgnoreSource(nameof(Quotation.Lane))]
    [MapperIgnoreSource(nameof(Quotation.CargoSpec))]
    [MapperIgnoreSource(nameof(Quotation.TemperatureSpec))]
    [MapperIgnoreSource(nameof(Quotation.Notes))]
    [MapperIgnoreSource(nameof(Quotation.ApprovedBy))]
    [MapperIgnoreSource(nameof(Quotation.ApprovedAt))]
    [MapperIgnoreSource(nameof(Quotation.RejectedBy))]
    [MapperIgnoreSource(nameof(Quotation.RejectedAt))]
    [MapperIgnoreSource(nameof(Quotation.RejectionReason))]
    [MapperIgnoreSource(nameof(Quotation.ApprovalEscalationReason))]
    [MapperIgnoreTarget(nameof(QuotationSummaryDto.CustomerName))]
    [MapperIgnoreTarget(nameof(QuotationSummaryDto.BranchName))]
    [MapperIgnoreTarget(nameof(QuotationSummaryDto.ReferenceNumber))]
    [MapperIgnoreTarget(nameof(QuotationSummaryDto.LineCount))]
    [MapperIgnoreTarget(nameof(QuotationSummaryDto.TotalSellingAmount))]
    public static partial QuotationSummaryDto ToSummaryDto(this Quotation entity);

    // =========================================================================
    // 3. QuotationLine -> QuotationLineDto
    // =========================================================================

    [MapperIgnoreSource(nameof(QuotationLine.DomainEvents))]
    [MapperIgnoreSource(nameof(QuotationLine.TariffRate))]
    [MapperIgnoreSource(nameof(QuotationLine.Quotation))]
    [MapperIgnoreSource(nameof(QuotationLine.SnapshotBuyingAmount))]
    [MapperIgnoreSource(nameof(QuotationLine.SnapshotSellingAmount))]
    // Target fields populated via TariffRate or pricing computation
    [MapperIgnoreTarget(nameof(QuotationLineDto.ChargeCode))]
    [MapperIgnoreTarget(nameof(QuotationLineDto.ChargeName))]
    [MapperIgnoreTarget(nameof(QuotationLineDto.Unit))]
    [MapperIgnoreTarget(nameof(QuotationLineDto.BuyingAmount))]
    [MapperIgnoreTarget(nameof(QuotationLineDto.SellingAmount))]
    [MapperIgnoreTarget(nameof(QuotationLineDto.IsPriceSnapshotted))]
    public static partial QuotationLineDto ToDto(this QuotationLine entity);

    // =========================================================================
    // 4. Overloads for Computed & Aggregated Fields
    // =========================================================================

    /// <summary>
    /// Full Quotation overload: Injects customer name, resolved route stops, 
    /// and aggregated buying/selling line amounts.
    /// </summary>
    public static QuotationDto ToDto(
        this Quotation entity,
        string customerName,
        string branchName,
        List<QuotationLineDto> lines,
        List<RouteStopSummaryDto>? stops = null)
    {
        var dto = entity.ToDto();
        dto.CustomerName = customerName;
        dto.BranchName = branchName;
        dto.ReferenceNumber = entity.ReferenceNumber;
        dto.Lines = lines;
        dto.Stops = stops ?? [];
        dto.TotalBuyingAmount = lines.Sum(l => l.BuyingAmount);
        dto.TotalSellingAmount = lines.Sum(l => l.SellingAmount);
        return dto;
    }

    /// <summary>
    /// Summary overload: Injects customer name and pre-computed line summaries.
    /// </summary>
    public static QuotationSummaryDto ToSummaryDto(
        this Quotation entity,
        string customerName,
        string branchName,
        int lineCount,
        decimal totalSellingAmount)
    {
        var dto = entity.ToSummaryDto();
        dto.CustomerName = customerName;
        dto.BranchName = branchName;
        dto.ReferenceNumber = entity.ReferenceNumber;
        dto.LineCount = lineCount;
        dto.FreightMode = entity.FreightMode;
        dto.TotalSellingAmount = totalSellingAmount;
        return dto;
    }

    /// <summary>
    /// Line overload: Computes (or retrieves snapshotted) prices and applies 
    /// TariffRate/ChargeCode metadata.
    /// </summary>
    public static QuotationLineDto ToDto(
        this QuotationLine entity,
        string chargeCode,
        string chargeName,
        string unit)
    {
        var (buying, selling) = entity.Price();
        var dto = entity.ToDto();

        dto.ChargeCode = chargeCode;
        dto.ChargeName = chargeName;
        dto.Unit = unit;
        dto.BuyingAmount = buying.Amount;
        dto.SellingAmount = selling.Amount;
        dto.Currency = buying.Currency;
        dto.IsPriceSnapshotted = entity.SnapshotBuyingAmount.HasValue;

        return dto;
    }
}
