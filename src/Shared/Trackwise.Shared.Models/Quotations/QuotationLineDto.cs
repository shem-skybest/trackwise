using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;

namespace Trackwise.Shared.Models.Quotations;

public class QuotationLineDto
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }
    public Guid TariffRateId { get; set; }

    // Charge Code & Description
    public string ChargeCode { get; set; } = default!;
    public string ChargeName { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public OperationalServiceType ServiceType { get; set; }

    // Pricing & Markup Inputs
    public decimal Quantity { get; set; }
    public decimal? FxRate { get; set; }
    public MarkupType MarkupType { get; set; }
    public decimal MarkupValue { get; set; }

    // Computed Amounts
    public decimal BuyingAmount { get; set; }
    public decimal SellingAmount { get; set; }
    public decimal MarginAmount => SellingAmount - BuyingAmount;
    public decimal MarginPercentage => BuyingAmount > 0
        ? Math.Round(((SellingAmount - BuyingAmount) / BuyingAmount) * 100m, 2)
        : 0m;

    public string Currency { get; set; } = default!;
    public bool IsPriceSnapshotted { get; set; }
    public string? Notes { get; set; }
}
