namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

using Stripe;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;

internal sealed class QuotationRatingEngine(
    ITariffRateResolver rateResolver
    //IFxRateService fxRateService
    ) // Inject FX conversion service
    : IQuotationRatingEngine
{
    private const string CodeLineHaul = "FRT";
    private const string CodeStopOff = "STOP";
    private const string CodePreCooling = "PRECOOL";
    private const string CodeTelematics = "IOT";

    public async Task AutoPriceAsync(Quotation quotation, Guid? customerContractId, CancellationToken ct = default)
    {
        var freightMode = quotation.FreightMode;

        // 1. Base Line-Haul / Freight Charge
        var baseRate = await rateResolver.ResolveRateAsync(
            customerId: quotation.CustomerId,
            contractId: customerContractId,
            chargeCode: CodeLineHaul,
            mode: freightMode,
            originLane: quotation.Lane.FirstPickup.LocationId.ToString(),
            destinationLane: quotation.Lane.FinalDrop.LocationId.ToString(),
            ct: ct);

        if (baseRate is not null)
        {
            //var fxRate = await ResolveFxRateAsync(baseRate, quotation, ct);

            quotation.AddLine(
                tariffRateId: baseRate.Id,
                category: baseRate.ChargeCode?.Category ?? ChargeCategory.Base,
                serviceType: OperationalServiceType.LineHaul,
                quantity: quotation.Lane.TotalDistanceKm,
                markupType: MarkupType.Percentage,
                markupValue: 12.0m,
                fxRate: null,
                notes: $"Base Line-Haul ({baseRate.Unit})");
        }

        // 2. Multi-Drop Intermediate Stop-Off Charges
        if (quotation.Lane.IntermediateDropCount > 0)
        {
            var stopOffRate = await rateResolver.ResolveRateAsync(
                customerId: quotation.CustomerId,
                contractId: customerContractId,
                chargeCode: CodeStopOff,
                mode: freightMode,
                ct: ct);

            if (stopOffRate is not null)
            {
                //var fxRate = await ResolveFxRateAsync(stopOffRate, quotation, ct);

                quotation.AddLine(
                    tariffRateId: stopOffRate.Id,
                    category: stopOffRate.ChargeCode?.Category ?? ChargeCategory.Accessorial,
                    serviceType: OperationalServiceType.Standard,
                    quantity: quotation.Lane.IntermediateDropCount,
                    markupType: MarkupType.Percentage,
                    markupValue: 15.0m,
                    fxRate: null,
                    notes: $"Stop-off charges for {quotation.Lane.IntermediateDropCount} intermediate drop(s).");
            }
        }

        // 3. Cold Chain Accessorial Charges
        if (quotation.IsTemperatureControlled)
        {
            if (quotation.TemperatureSpec!.Zone != TemperatureZone.Ambient)
            {
                var precoolRate = await rateResolver.ResolveRateAsync(
                    customerId: quotation.CustomerId,
                    contractId: customerContractId,
                    chargeCode: CodePreCooling,
                    mode: freightMode,
                    ct: ct);

                if (precoolRate is not null)
                {
                    //var fxRate = await ResolveFxRateAsync(precoolRate, quotation, ct);

                    quotation.AddLine(
                        tariffRateId: precoolRate.Id,
                        category: precoolRate.ChargeCode?.Category ?? ChargeCategory.Accessorial,
                        serviceType: OperationalServiceType.PreCoolingAndPTI,
                        quantity: 1,
                        markupType: MarkupType.Flat,
                        markupValue: 0m,
                        fxRate: null,
                        notes: "Reefer Pre-Trip Inspection and thermal pull-down[cite: 1].");
                }
            }

            if (quotation.TemperatureSpec.RequiresContinuousTelematics)
            {
                var telematicsRate = await rateResolver.ResolveRateAsync(
                    customerId: quotation.CustomerId,
                    contractId: customerContractId,
                    chargeCode: CodeTelematics,
                    mode: freightMode,
                    ct: ct);

                if (telematicsRate is not null)
                {
                    //var fxRate = await ResolveFxRateAsync(telematicsRate, quotation, ct);

                    quotation.AddLine(
                        tariffRateId: telematicsRate.Id,
                        category: telematicsRate.ChargeCode?.Category ?? ChargeCategory.Accessorial,
                        serviceType: OperationalServiceType.ContinuousTelematics,
                        quantity: 1,
                        markupType: MarkupType.Flat,
                        markupValue: 0m,
                        fxRate: null,
                        notes: "Continuous IoT temperature logger & telemetry tracking[cite: 1].");
                }
            }
        }
    }

    /// <summary>
    /// Resolves the FX rate between the rate's currency and the quotation's target currency.
    /// Returns null if currencies are identical, or the specific conversion rate if they differ.
    /// </summary>
    //private async Task<decimal?> ResolveFxRateAsync(
    //    TariffRate rate,
    //    Quotation quotation,
    //    CancellationToken ct)
    //{
    //    var buyingCurrency = rate.Currency; // or rate.Tariff?.Currency
    //    var quoteCurrency = quotation.Currency;

    //    if (string.Equals(buyingCurrency, quoteCurrency, StringComparison.OrdinalIgnoreCase))
    //    {
    //        return null; // Same currency; no FX conversion needed
    //    }

    //    // Resolves contract-pegged FX rate if exists, otherwise spot rate from FX tables
    //    return await fxRateService.GetExchangeRateAsync(
    //            fromCurrency: buyingCurrency,
    //            toCurrency: quoteCurrency,
    //            ct: ct);
    //}
}
