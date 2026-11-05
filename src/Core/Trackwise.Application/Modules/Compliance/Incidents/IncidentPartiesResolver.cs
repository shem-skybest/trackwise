using Trackwise.Domain.Core;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Compliance.Incidents;

/// <summary>Checks every id the report names exists, and takes the client from the trip when none is given.</summary>
internal static class IncidentPartiesResolver
{
    public static async Task<Result<IncidentParties>> ResolveAsync(
        ITenantUnitOfWork uow, IncidentFields fields, CancellationToken ct)
    {
        var customerId = fields.CustomerId;
        if (fields.TripId is { } tripId)
        {
            var trip = await uow.Repository<Trip>().GetByIdAsync(tripId, ct);
            if (trip is null)
            {
                return Result<IncidentParties>.Fail("Could not find that trip.");
            }

            customerId ??= trip.Stops
                .Where(s => s.Type == TripStopType.DropOff)
                .Select(s => s.Load.CustomerId)
                .Distinct()
                .ToList() is [var only]
                ? only
                : null;
        }

        if (await MissingAsync<Customer>(uow, customerId, ct)
            || await MissingAsync<Truck>(uow, fields.TruckId, ct)
            || await MissingAsync<Employee>(uow, fields.DriverId, ct)
            || await MissingAsync<Helper>(uow, fields.HelperId, ct)
            || await MissingAsync<Helper>(uow, fields.SecondHelperId, ct))
        {
            return Result<IncidentParties>.Fail("A client, truck or crew member on the report no longer exists.");
        }

        return Result<IncidentParties>.Ok(new IncidentParties(
            fields.TripId, customerId, fields.TruckId, fields.DriverId, fields.HelperId, fields.SecondHelperId));
    }

    private static async Task<bool> MissingAsync<T>(ITenantUnitOfWork uow, Guid? id, CancellationToken ct)
        where T : Entity, ITenantEntity =>
        id is { } value && !await uow.Repository<T>().Query().AnyAsync(e => e.Id == value, ct);
}
