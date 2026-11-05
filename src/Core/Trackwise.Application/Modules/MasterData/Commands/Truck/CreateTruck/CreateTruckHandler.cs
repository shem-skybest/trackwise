using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateTruckHandler(ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateTruckCommand, Result<TruckDto>>
{
    public async Task<Result<TruckDto>> Handle(CreateTruckCommand req, CancellationToken ct)
    {
        var limitResult = await CheckTruckLimitAsync(ct);
        if (!limitResult.IsSuccess)
        {
            return Result<TruckDto>.Fail(limitResult.Error!, limitResult.ErrorCode!);
        }

        var number = Truck.NormalizeCode(req.Number);

        if (number.Length == 0)
        {
            number = Truck.NormalizeCode(await codes.NextAsync(nameof(Truck), "TRK", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted truck is allowed, but the steward should be told what they are reusing
            // rather than silently colliding with history.
            var clash = await tenantUow.Repository<Truck>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Number == number, ct);

            if (clash is not null)
            {
                return Result<TruckDto>.Fail(clash.DeletedAt is null
                    ? $"A truck with number '{number}' already exists"
                    : $"Number '{number}' belonged to a deleted truck. Restore that truck or choose another number.");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<TruckDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        Employee? mainDriver = null;
        if (req.MainDriverId is { } mainDriverId)
        {
            mainDriver = await tenantUow.Repository<Employee>().GetByIdAsync(mainDriverId, ct);

            if (mainDriver is null)
            {
                return Result<TruckDto>.Fail($"Could not find a driver with ID {mainDriverId}");
            }
        }

        Employee? secondaryDriver = null;
        if (req.SecondaryDriverId is { } secondaryDriverId)
        {
            secondaryDriver = await tenantUow.Repository<Employee>().GetByIdAsync(secondaryDriverId, ct);

            if (secondaryDriver is null)
            {
                return Result<TruckDto>.Fail($"Could not find a driver with ID {secondaryDriverId}");
            }
        }

        var name = string.IsNullOrWhiteSpace(req.Name)
            ? DefaultName(req.Year, req.Make, req.Model, number)
            : req.Name.Trim();

        var truck = new Truck
        {
            Number = number,
            Name = name,
            Type = req.Type,
            VehicleCapacity = req.VehicleCapacity ?? 0,
            TonnageClass = req.TonnageClass,
            GrossVehicleWeightKg = req.GrossVehicleWeightKg,
            Make = req.Make,
            Model = req.Model,
            Year = req.Year,
            Vin = req.Vin,
            LicensePlate = req.LicensePlate,
            LicensePlateState = req.LicensePlateState,
            AssetCode = req.AssetCode,
            ConductionSticker = req.ConductionSticker,
            EngineNumber = req.EngineNumber,
            AcquiredOn = req.AcquiredOn,
            Supplier = req.Supplier,
            Ownership = req.Ownership,
            AdrEquipment = req.AdrEquipment?.ToDomain() ?? Domain.Primitives.ValueObjects.AdrEquipment.None,
            IsHazmatPlacarded = req.IsHazmatPlacarded,
            MainDriver = mainDriver,
            SecondaryDriver = secondaryDriver,
            BranchId = req.BranchId
        };

        await tenantUow.Repository<Truck>().AddAsync(truck, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<TruckDto>.Ok(truck.ToDto());
    }

    /// <summary>
    /// Trucks have no descriptive name in the legacy data, so a blank name falls back to
    /// make/model/year, and to the unit number itself if even those are blank.
    /// </summary>
    private static string DefaultName(int? year, string? make, string? model, string number)
    {
        var parts = new[] { year?.ToString(), make, model }.Where(p => !string.IsNullOrWhiteSpace(p));
        var fallback = string.Join(' ', parts).Trim();
        return fallback.Length == 0 ? number : fallback;
    }

    private async Task<Result> CheckTruckLimitAsync(CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();

        if (tenant.Subscription is null)
        {
            return Result.Ok();
        }

        var maxTrucks = tenant.Subscription.Plan.MaxTrucks;

        if (maxTrucks is null)
        {
            return Result.Ok();
        }

        var currentCount = await tenantUow.Repository<Truck>().Query().CountAsync(ct);

        if (currentCount >= maxTrucks.Value)
        {
            return Result.Fail(
                $"Your {tenant.Subscription.Plan.Name} plan allows a maximum of {maxTrucks.Value} trucks. Please upgrade your plan to add more.",
                ErrorCodes.ResourceLimitReached);
        }

        return Result.Ok();
    }
}
