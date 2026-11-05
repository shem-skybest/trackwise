using System.Text.Json;
using Trackwise.Application.Abstractions.FuelCards;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Integrations.FuelCards.Common;

namespace Trackwise.Infrastructure.Integrations.FuelCards.Providers.Wex;

internal static class WexMapper
{
    public static FuelCardTransactionData? ToTransactionData(WexTransaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.TransactionId) || transaction.TransactionDate is null || transaction.Amount is null)
        {
            return null;
        }

        return new FuelCardTransactionData
        {
            ExternalTransactionId = transaction.TransactionId,
            TransactionDate = DateTime.SpecifyKind(transaction.TransactionDate.Value, DateTimeKind.Utc),
            Amount = transaction.Amount.Value,
            Currency = transaction.Currency ?? "USD",
            Quantity = transaction.Quantity,
            QuantityUnit = MapUnit(transaction.UnitOfMeasure),
            PricePerUnit = transaction.PricePerUnit,
            ProductCategory = transaction.ProductDescription,
            MerchantName = transaction.MerchantName,
            MerchantCity = transaction.MerchantCity,
            PurchaseJurisdiction = JurisdictionMapper.FromMerchant(transaction.MerchantCountry, transaction.MerchantState),
            CardNumberMasked = transaction.CardNumber,
            ExternalCardId = transaction.CardId ?? transaction.CardNumber,
            UnitNumber = transaction.VehicleNumber,
            DriverName = transaction.DriverName,
            RawJson = JsonSerializer.Serialize(transaction)
        };
    }

    private static VolumeUnit? MapUnit(string? unitOfMeasure)
    {
        return unitOfMeasure?.Trim().ToUpperInvariant() switch
        {
            "GAL" or "GALLON" or "GALLONS" => VolumeUnit.Gallons,
            "L" or "LTR" or "LITER" or "LITERS" or "LITRE" or "LITRES" => VolumeUnit.Liters,
            null or "" => VolumeUnit.Gallons,
            _ => null
        };
    }
}
