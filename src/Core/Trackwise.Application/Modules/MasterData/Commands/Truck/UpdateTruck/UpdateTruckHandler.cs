using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateTruckHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateTruckCommand, Result>
{
    public async Task<Result> Handle(UpdateTruckCommand req, CancellationToken ct)
    {
        var truck = await tenantUow.Repository<Truck>().GetByIdAsync(req.Id, ct);
        if (truck is null)
        {
            return Result.Fail($"Could not find truck with ID '{req.Id}'");
        }

        if (req.BranchId is { } branchId && branchId != truck.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        if (await SetDriverAsync(truck, req.MainDriverId, true) is { } fail1)
        {
            return fail1;
        }

        if (await SetDriverAsync(truck, req.SecondaryDriverId, false) is { } fail2)
        {
            return fail2;
        }

        truck.Name = req.Name;
        truck.Type = req.Type;
        truck.Status = req.Status;
        truck.VehicleCapacity = req.VehicleCapacity ?? truck.VehicleCapacity;
        truck.TonnageClass = req.TonnageClass;
        truck.GrossVehicleWeightKg = req.GrossVehicleWeightKg;
        truck.Make = req.Make;
        truck.Model = req.Model;
        truck.Year = req.Year;
        truck.Vin = req.Vin;
        truck.LicensePlate = req.LicensePlate;
        truck.LicensePlateState = req.LicensePlateState;
        truck.AssetCode = req.AssetCode;
        truck.ConductionSticker = req.ConductionSticker;
        truck.EngineNumber = req.EngineNumber;
        truck.AcquiredOn = req.AcquiredOn;
        truck.Supplier = req.Supplier;
        truck.Ownership = req.Ownership;
        truck.IsHazmatPlacarded = req.IsHazmatPlacarded;
        truck.BranchId = req.BranchId;
        truck.IsActive = req.IsActive;

        if (req.AdrEquipment is not null)
        {
            truck.AdrEquipment = req.AdrEquipment.ToDomain();
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task<Result?> SetDriverAsync(Truck truck, Guid? newDriverId, bool isMain)
    {
        var currentId = isMain ? truck.MainDriverId : truck.SecondaryDriverId;
        if (newDriverId == currentId)
        {
            return null;
        }

        if (newDriverId is null)
        {
            if (isMain)
            {
                truck.MainDriver = null;
            }
            else
            {
                truck.SecondaryDriver = null;
            }

            return null;
        }

        var driver = await tenantUow.Repository<Employee>().GetByIdAsync(newDriverId.Value);
        if (driver is null)
        {
            return Result.Fail($"Could not find a driver with ID {newDriverId}");
        }

        if (isMain)
        {
            truck.MainDriver = driver;
        }
        else
        {
            truck.SecondaryDriver = driver;
        }

        return null;
    }
}
