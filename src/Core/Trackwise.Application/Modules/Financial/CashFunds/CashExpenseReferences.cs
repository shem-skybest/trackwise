using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds;

/// <summary>
/// Checks what a fund expense points at and fills in what the trip already knows: an expense on a
/// trip is on that trip's truck, and paid by its driver unless Ops says otherwise.
/// </summary>
internal static class CashExpenseReferences
{
    public static async Task<Result<CashExpenseSpec>> ResolveAsync(
        ITenantUnitOfWork tenantUow, CashExpenseSpec spec, CancellationToken ct)
    {
        var today = tenantUow.GetCurrentTenant().Settings.LocalDate(DateTime.UtcNow);
        if (spec.SpentOn > today)
        {
            return Result<CashExpenseSpec>.Fail("An expense can't be dated in the future.");
        }

        if (spec.TripId is { } tripId)
        {
            var trip = await tenantUow.Repository<Trip>().GetByIdAsync(tripId, ct);
            if (trip is null)
            {
                return Result<CashExpenseSpec>.Fail($"Could not find the trip '{tripId}'.");
            }

            if (spec.TruckId is { } truckId && trip.TruckId is { } tripTruckId && truckId != tripTruckId)
            {
                return Result<CashExpenseSpec>.Fail("That trip ran on a different truck.");
            }

            spec = spec with
            {
                TruckId = spec.TruckId ?? trip.TruckId,
                SpentById = spec.SpentById ?? trip.DriverId
            };
        }

        if (spec.TruckId is { } id && await tenantUow.Repository<Truck>().GetByIdAsync(id, ct) is null)
        {
            return Result<CashExpenseSpec>.Fail($"Could not find the truck '{id}'.");
        }

        if (spec.SpentById is { } payerId && await tenantUow.Repository<Employee>().GetByIdAsync(payerId, ct) is null)
        {
            return Result<CashExpenseSpec>.Fail($"Could not find the employee '{payerId}'.");
        }

        return Result<CashExpenseSpec>.Ok(spec);
    }
}
