using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class VehiclePermitMapper
{
    /// <summary>
    /// The vehicle's code and name come in from a lookup the caller has already batched: lazy loading
    /// is on, so reading <c>entity.Truck</c> here would issue a query per row.
    /// </summary>
    public static VehiclePermitDto ToDto(
        this VehiclePermit entity,
        DateOnly today,
        bool isCurrent,
        (string Code, string Name)? vehicle = null)
    {
        return new VehiclePermitDto
        {
            Id = entity.Id,
            TruckId = entity.TruckId,
            TrailerId = entity.TrailerId,
            VehicleCode = vehicle?.Code,
            VehicleName = vehicle?.Name,
            Type = entity.Type,
            Number = entity.Number,
            Issuer = entity.Issuer,
            IssuedOn = entity.IssuedOn,
            ExpiresOn = entity.ExpiresOn,
            Cost = entity.Cost,
            Notes = entity.Notes,
            DaysUntilExpiry = entity.DaysUntilExpiry(today),
            IsCurrent = isCurrent,
            CreatedAt = entity.CreatedAt
        };
    }
}
