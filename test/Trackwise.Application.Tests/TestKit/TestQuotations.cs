using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;

namespace Trackwise.Application.Tests.TestKit;

internal static class TestQuotations
{
    /// <summary>
    /// Adds the Base freight line every quotation needs before it can be submitted or approved.
    /// </summary>
    public static QuotationLine AddBaseLine(Quotation quotation)
    {
        var chargeCode = ChargeCode.Create("FRT", "Freight", ChargeCategory.Base, [FreightMode.Domestic], "km");
        chargeCode.Currency = quotation.Currency;

        var tariffRate = new TariffRate
        {
            ChargeCode = chargeCode,
            PricingModel = PricingModel.PerUnit,
            BaseAmount = 10m,
            Unit = "km"
        };

        var line = quotation.AddLine(
            tariffRateId: tariffRate.Id,
            category: ChargeCategory.Base,
            serviceType: OperationalServiceType.LineHaul,
            quantity: 100m,
            markupType: MarkupType.Percentage,
            markupValue: 10m,
            fxRate: null,
            notes: null);

        // Issue() prices each line through this navigation, which EF would lazy-load.
        typeof(QuotationLine).GetProperty(nameof(QuotationLine.TariffRate))?.SetValue(line, tariffRate);
        return line;
    }

    /// <summary>A priced quotation taken through approval, issue and acceptance - what a booking needs.</summary>
    public static Quotation Accepted(Quotation quotation, string approvedBy = "manager@test.com")
    {
        AddBaseLine(quotation);
        quotation.Approve(approvedBy);
        quotation.Issue(TimeSpan.FromDays(30));
        quotation.Accept();
        return quotation;
    }
}
