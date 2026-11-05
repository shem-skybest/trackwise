using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Dispatch;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class DispatchTripHandler(
    ITenantUnitOfWork tenantUow,
    IDispatchEligibilityService eligibilityService,
    ITripCrewService crewService,
    ILogger<DispatchTripHandler> logger)
    : IAppRequestHandler<DispatchTripCommand, Result<DispatchTripResultDto>>
{
    public async Task<Result<DispatchTripResultDto>> Handle(DispatchTripCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);

        if (trip is null)
        {
            return Result<DispatchTripResultDto>.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        // Hard eligibility gate at dispatch â€” every load on the trip must be carryable
        // by the assigned truck/driver. Planning steps (assignment, trip creation) don't
        // enforce this; the dispatcher is the final commit.
        // Every load is checked against the same truck and driver, so the same warning comes back
        // once per load; a set keeps each one once.
        var warnings = new HashSet<string>();

        if (trip.TruckId.HasValue)
        {
            // Distinct load IDs across stops (a load may have both pickup + dropoff stops).
            var loadIds = trip.Stops.Select(s => s.LoadId).Distinct();
            foreach (var loadId in loadIds)
            {
                // The trip's own driver, not the truck's main driver: crews rotate across trucks
                // daily, and most units have no standing driver at all.
                var eligibility = await eligibilityService.CheckAsync(
                    trip.TruckId.Value, loadId, trip.DriverId, ct);
                if (!eligibility.IsEligible)
                {
                    var reasons = string.Join("; ",
                        eligibility.Issues
                            .Where(i => i.Severity == EligibilitySeverity.Error)
                            .Select(i => i.Message));
                    return Result<DispatchTripResultDto>.Fail(
                        $"Cannot dispatch trip â€” load '{loadId}' not eligible: {reasons}");
                }

                warnings.UnionWith(eligibility.Issues
                    .Where(i => i.Severity == EligibilitySeverity.Warning)
                    .Select(i => i.Message));
            }
        }

        if (await crewService.FindCrewConflictAsync(trip, ct) is { } conflict)
        {
            return Result<DispatchTripResultDto>.Fail(conflict);
        }

        try
        {
            trip.Dispatch();
        }
        catch (InvalidOperationException ex)
        {
            return Result<DispatchTripResultDto>.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Dispatched trip '{TripName}' with ID '{TripId}'",
            trip.Name, trip.Id);

        return Result<DispatchTripResultDto>.Ok(new DispatchTripResultDto { Warnings = warnings.ToArray() });
    }
}
