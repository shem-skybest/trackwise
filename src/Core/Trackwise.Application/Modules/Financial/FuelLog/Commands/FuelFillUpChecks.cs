using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Commands;

/// <summary>
/// Turns a typed fill-up into one the log can hold, catching the slips Monday let through: liters
/// and amount typed into each other's column, the same receipt logged twice, an odometer that goes
/// backwards, a trip on another truck.
/// </summary>
internal static class FuelFillUpChecks
{
    /// <summary>Pesos per liter. Diesel ran ₱86-93 in 2026; outside this band a column was mistyped.</summary>
    private const decimal MinPesosPerLiter = 30;
    private const decimal MaxPesosPerLiter = 150;

    public static async Task<Result<FuelFillUp>> ResolveAsync(
        ITenantUnitOfWork tenantUow, FuelFillUpCommand cmd, Guid? fillUpId, CancellationToken ct)
    {
        var settings = tenantUow.GetCurrentTenant().Settings;
        var currency = settings.Currency.ToString();

        if (cmd.FilledAt > DateTime.UtcNow.AddMinutes(10))
        {
            return Result<FuelFillUp>.Fail("A fill-up can't be in the future.");
        }

        var pricePerLiter = cmd.Amount / cmd.Liters;
        if (currency == "PHP" && pricePerLiter is < MinPesosPerLiter or > MaxPesosPerLiter)
        {
            return Result<FuelFillUp>.Fail(
                $"That works out to ₱{pricePerLiter:N2} a liter. Check the liters and the amount.");
        }

        if (await tenantUow.Repository<Truck>().GetByIdAsync(cmd.TruckId, ct) is null)
        {
            return Result<FuelFillUp>.Fail($"Could not find the truck '{cmd.TruckId}'.");
        }

        if (cmd.TripId is { } tripId)
        {
            var trip = await tenantUow.Repository<Trip>().GetByIdAsync(tripId, ct);
            if (trip is null)
            {
                return Result<FuelFillUp>.Fail($"Could not find the trip '{tripId}'.");
            }

            if (trip.TruckId is { } tripTruckId && tripTruckId != cmd.TruckId)
            {
                return Result<FuelFillUp>.Fail("That trip ran on a different truck.");
            }
        }

        var dayStart = settings.ToUtc(settings.LocalDate(cmd.FilledAt), TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var duplicate = await FuelLogReader.FillUps(tenantUow)
            .Where(e => e.TruckId == cmd.TruckId && e.Id != fillUpId
                && e.ExpenseDate >= dayStart && e.ExpenseDate < dayEnd
                && e.Quantity == cmd.Liters && e.Amount.Amount == cmd.Amount)
            .Select(e => (long?)e.Number)
            .FirstOrDefaultAsync(ct);
        if (duplicate is { } number)
        {
            return Result<FuelFillUp>.Fail(
                $"Fill-up #{number} on this truck that day has the same liters and amount. It looks already logged.");
        }

        if (cmd.OdometerReading is { } reading)
        {
            var outOfOrder = await CheckOdometerAsync(tenantUow, cmd, reading, fillUpId, settings, ct);
            if (outOfOrder is not null)
            {
                return Result<FuelFillUp>.Fail(outOfOrder);
            }
        }

        return Result<FuelFillUp>.Ok(new FuelFillUp(
            cmd.TruckId,
            DateTime.SpecifyKind(cmd.FilledAt, DateTimeKind.Utc),
            cmd.Liters,
            new Money { Amount = cmd.Amount, Currency = currency },
            cmd.FleetCard,
            cmd.OdometerReading,
            cmd.TripId,
            Clean(cmd.Station),
            Clean(cmd.Notes)));
    }

    /// <summary>A reading must sit between the truck's readings before and after it.</summary>
    private static async Task<string?> CheckOdometerAsync(
        ITenantUnitOfWork tenantUow, FuelFillUpCommand cmd, int reading, Guid? fillUpId,
        TenantSettings settings, CancellationToken ct)
    {
        var readings = FuelLogReader.FillUps(tenantUow)
            .Where(e => e.TruckId == cmd.TruckId && e.OdometerReading != null && e.Id != fillUpId);

        var before = await readings
            .Where(e => e.ExpenseDate <= cmd.FilledAt)
            .OrderByDescending(e => e.ExpenseDate)
            .Select(e => new { e.ExpenseDate, Reading = e.OdometerReading!.Value })
            .FirstOrDefaultAsync(ct);
        if (before is not null && reading < before.Reading)
        {
            return $"The odometer read {before.Reading:N0} km on {Day(before.ExpenseDate)}, so {reading:N0} is too low. Check the reading.";
        }

        var after = await readings
            .Where(e => e.ExpenseDate > cmd.FilledAt)
            .OrderBy(e => e.ExpenseDate)
            .Select(e => new { e.ExpenseDate, Reading = e.OdometerReading!.Value })
            .FirstOrDefaultAsync(ct);
        if (after is not null && reading > after.Reading)
        {
            return $"The odometer read {after.Reading:N0} km later, on {Day(after.ExpenseDate)}, so {reading:N0} is too high. Check the reading.";
        }

        return null;

        string Day(DateTime utc) => settings.LocalDate(utc).ToString("d MMM", CultureInfo.InvariantCulture);
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
