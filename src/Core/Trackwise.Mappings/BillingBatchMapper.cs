using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class BillingBatchMapper
{
    /// <summary>Header only: list reads leave the trips and charges unloaded.</summary>
    [MapperIgnoreSource(nameof(BillingBatch.DomainEvents))]
    [MapperIgnoreSource(nameof(BillingBatch.Customer))]
    [MapperIgnoreSource(nameof(BillingBatch.Trips))]
    [MapperIgnoreSource(nameof(BillingBatch.Charges))]
    [MapperIgnoreSource(nameof(BillingBatch.Period))]
    [MapperIgnoreSource(nameof(BillingBatch.UpdatedAt))]
    [MapperIgnoreSource(nameof(BillingBatch.UpdatedBy))]
    [MapperIgnoreTarget(nameof(BillingBatchDto.CustomerName))]
    [MapperIgnoreTarget(nameof(BillingBatchDto.Trips))]
    [MapperIgnoreTarget(nameof(BillingBatchDto.Charges))]
    private static partial BillingBatchDto ToHeaderDto(this BillingBatch entity);

    public static BillingBatchDto ToDto(this BillingBatch entity, string? customerName) =>
        entity.ToHeaderDto() with { CustomerName = customerName };

    [MapperIgnoreSource(nameof(BillingBatchTrip.DomainEvents))]
    [MapperIgnoreSource(nameof(BillingBatchTrip.Id))]
    [MapperIgnoreSource(nameof(BillingBatchTrip.BillingBatchId))]
    [MapperIgnoreSource(nameof(BillingBatchTrip.Trip))]
    public static partial BillingBatchTripDto ToDto(this BillingBatchTrip entity);

    /// <summary>A trip not on a batch yet, with the same checks a batch would make of it.</summary>
    [MapperIgnoreSource(nameof(BillableTrip.CustomerId))]
    [MapProperty(nameof(BillableTrip.Status), nameof(BillingBatchTripDto.TripStatus))]
    public static partial BillingBatchTripDto ToDto(this BillableTrip trip);

    [MapperIgnoreSource(nameof(BillingBatchCharge.DomainEvents))]
    [MapperIgnoreSource(nameof(BillingBatchCharge.BillingBatchId))]
    [MapperIgnoreSource(nameof(BillingBatchCharge.UpdatedAt))]
    [MapperIgnoreSource(nameof(BillingBatchCharge.UpdatedBy))]
    [MapperIgnoreTarget(nameof(BillingBatchChargeDto.TripNumber))]
    public static partial BillingBatchChargeDto ToDto(this BillingBatchCharge entity);
}
