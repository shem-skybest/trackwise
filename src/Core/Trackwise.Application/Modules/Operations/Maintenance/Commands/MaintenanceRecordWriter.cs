using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal static class MaintenanceRecordWriter
{
    /// <summary>
    /// Writes the fields onto the record, replaces its parts and moves its schedule on. Every lookup
    /// runs before the first change, so a failure leaves the record untouched.
    /// </summary>
    public static async Task<Result> ApplyAsync(
        ITenantUnitOfWork tenantUow, MaintenanceRecord record, IMaintenanceRecordFields req, CancellationToken ct)
    {
        if (await tenantUow.Repository<Truck>().GetByIdAsync(req.TruckId, ct) is null)
        {
            return Result.Fail("Truck not found.");
        }

        if (req.PerformedById is { } performedById
            && await tenantUow.Repository<Employee>().GetByIdAsync(performedById, ct) is null)
        {
            return Result.Fail("Performed by employee not found.");
        }

        var schedule = await FindScheduleAsync(tenantUow, req, ct);
        if (req.MaintenanceScheduleId.HasValue && schedule is null)
        {
            return Result.Fail("That maintenance schedule isn't one of this truck's.");
        }

        record.TruckId = req.TruckId;
        record.MaintenanceScheduleId = schedule?.Id;
        record.MaintenanceType = req.Type;
        record.VehicleSystem = req.VehicleSystem;
        record.Description = req.Description;
        record.ServiceDate = req.ServiceDate;
        record.OdometerReading = req.OdometerReading;
        record.EngineHours = req.EngineHours;
        record.RequisitionNumber = req.RequisitionNumber;
        record.VendorName = req.VendorName;
        record.InvoiceNumber = req.InvoiceNumber;
        record.PerformedById = req.PerformedById;
        record.WorkPerformed = req.Notes;

        var partRepository = tenantUow.Repository<MaintenancePart>();
        foreach (var old in record.Parts.ToList())
        {
            partRepository.Delete(old);
        }

        var specs = req.Parts.Select(p =>
            new MaintenancePartSpec(p.PartName, p.Description, p.PartNumber, p.Quantity, p.UnitCost));
        foreach (var part in record.ReplaceParts(specs, req.LaborCost, req.PartsCost))
        {
            await partRepository.AddAsync(part, ct);
        }

        schedule?.RecordService(req.ServiceDate, req.OdometerReading, req.EngineHours);
        return Result.Ok();
    }

    private static async Task<MaintenanceSchedule?> FindScheduleAsync(
        ITenantUnitOfWork tenantUow, IMaintenanceRecordFields req, CancellationToken ct)
    {
        var schedules = tenantUow.Repository<MaintenanceSchedule>().Query().Where(s => s.TruckId == req.TruckId);
        if (req.MaintenanceScheduleId is { } scheduleId)
        {
            return await schedules.FirstOrDefaultAsync(s => s.Id == scheduleId, ct);
        }

        var matching = await schedules
            .Where(s => s.IsActive && s.MaintenanceType == req.Type)
            .Take(2)
            .ToListAsync(ct);
        return matching.Count == 1 ? matching[0] : null;
    }
}
