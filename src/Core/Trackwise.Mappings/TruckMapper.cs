using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class TruckMapper
{
    /// <summary>
    ///     Maps a Truck entity to TruckDto without loads.
    /// </summary>
    public static TruckDto ToDto(this Truck entity)
    {
        return new TruckDto
        {
            Id = entity.Id,
            Number = entity.Number,
            Name = entity.Name,
            Type = entity.Type,
            Status = entity.Status,
            Make = entity.Make,
            Model = entity.Model,
            Year = entity.Year,
            Vin = entity.Vin,
            LicensePlate = entity.LicensePlate,
            LicensePlateState = entity.LicensePlateState,
            AssetCode = entity.AssetCode,
            ConductionSticker = entity.ConductionSticker,
            EngineNumber = entity.EngineNumber,
            AcquiredOn = entity.AcquiredOn,
            Supplier = entity.Supplier,
            Ownership = entity.Ownership,
            AdrEquipment = entity.AdrEquipment.ToDto(),
            IsHazmatPlacarded = entity.IsHazmatPlacarded,
            CurrentLocation = entity.CurrentLocation,
            CurrentAddress = entity.CurrentAddress,
            VehicleCapacity = entity.VehicleCapacity,
            TonnageClass = entity.TonnageClass,
            GrossVehicleWeightKg = entity.GrossVehicleWeightKg,
            Loads = [],
            MainDriver = entity.MainDriver?.ToDto(),
            SecondaryDriver = entity.SecondaryDriver?.ToDto(),
            BranchId = entity.BranchId,
            IsActive = entity.IsActive,
            MergedIntoId = entity.MergedIntoId,
            DeletedAt = entity.DeletedAt,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy,
        };
    }

    /// <summary>
    /// Adds the owning branch name from a lookup the caller has already batched. Taking the name
    /// through a dictionary rather than <c>entity.Branch</c> is deliberate: lazy loading is on, so
    /// reading the navigation here would issue a query per row.
    /// </summary>
    public static TruckDto ToDto(this Truck entity, IReadOnlyDictionary<Guid, string> branchNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var name))
        {
            dto = dto with { BranchName = name };
        }

        return dto;
    }

    public static AdrEquipmentDto ToDto(this AdrEquipment entity) => new()
    {
        IsAdrCertified = entity.IsAdrCertified,
        AdrCertExpiresAt = entity.AdrCertExpiresAt,
        AllowedClasses = entity.AllowedClasses.ToClasses(),
        OrangePlateNumber = entity.OrangePlateNumber
    };

    public static AdrEquipment ToDomain(this AdrEquipmentDto dto) => new()
    {
        IsAdrCertified = dto.IsAdrCertified,
        AdrCertExpiresAt = dto.AdrCertExpiresAt,
        AllowedClasses = dto.AllowedClasses.ToFlags(),
        OrangePlateNumber = dto.OrangePlateNumber
    };

    /// <summary>
    ///     Maps a Truck entity to TruckDto with loads.
    /// </summary>
    public static TruckDto ToDto(this Truck entity, IEnumerable<LoadDto> loads)
    {
        var dto = entity.ToDto();
        dto.Loads = loads;
        return dto;
    }
}
