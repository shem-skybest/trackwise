using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class FuelFillUpMapper
{
    /// <summary>
    /// A fuel expense as a fill-up. The truck and trip numbers and the distance since the previous
    /// reading span other rows, so the handler looks them up for the whole page and passes them in.
    /// </summary>
    public static FuelFillUpDto ToFuelFillUpDto(
        this TruckExpense entity, string? truckNumber, long? tripNumber, int? kmSincePrevious, decimal? kmPerLiter) =>
        new()
        {
            Id = entity.Id,
            Number = entity.Number,
            TruckId = entity.TruckId,
            TruckNumber = truckNumber,
            TripId = entity.TripId,
            TripNumber = tripNumber,
            FilledAt = entity.ExpenseDate,
            Liters = entity.Quantity ?? 0,
            PricePerLiter = entity.PricePerUnit ?? 0,
            Amount = entity.Amount.Amount,
            Currency = entity.Amount.Currency,
            FleetCard = entity.FleetCard,
            OdometerReading = entity.OdometerReading,
            KmSincePrevious = kmSincePrevious,
            KmPerLiter = kmPerLiter,
            Station = entity.VendorName,
            Notes = entity.Notes,
            HasReceipt = !string.IsNullOrEmpty(entity.ReceiptBlobPath),
            Status = entity.Status,
            FlaggedReason = entity.RejectionReason
        };
}
