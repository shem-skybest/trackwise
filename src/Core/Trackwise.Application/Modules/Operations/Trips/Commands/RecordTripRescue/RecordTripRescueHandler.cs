using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordTripRescueHandler(
    ITenantUnitOfWork tenantUow,
    ITripCrewService crewService,
    ICrewPlanService crewPlan,
    ICurrentUserService currentUser,
    ILogger<RecordTripRescueHandler> logger)
    : IAppRequestHandler<RecordTripRescueCommand, Result<TripRescueDto>>
{
    public async Task<Result<TripRescueDto>> Handle(RecordTripRescueCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result<TripRescueDto>.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        var truck = await tenantUow.Repository<Truck>().GetByIdAsync(req.TruckId, ct);
        if (truck is null)
        {
            return Result<TripRescueDto>.Fail("Could not find the rescue truck.");
        }

        var crew = await crewService.ResolveAsync(req.DriverId, req.HelperId, req.SecondHelperId, ct);
        if (!crew.IsSuccess)
        {
            return Result<TripRescueDto>.Fail(crew.Error!);
        }

        if (req.IncidentId is { } incidentId)
        {
            var incident = await tenantUow.Repository<Incident>().GetByIdAsync(incidentId, ct);
            if (incident is null || incident.TripId != trip.Id)
            {
                return Result<TripRescueDto>.Fail("That incident isn't on this trip.");
            }
        }

        var people = new[] { req.DriverId, req.HelperId, req.SecondHelperId }.OfType<Guid>().ToList();
        if (await crewPlan.FindUnavailableAsync(crewPlan.TripDay(trip.GetLoads()), people, ct) is { } unavailable)
        {
            return Result<TripRescueDto>.Fail(unavailable);
        }

        TripRescue rescue;
        try
        {
            rescue = trip.RecordRescue(
                new RescueSpec(
                    truck,
                    crew.Value!.Driver!,
                    crew.Value.Helper,
                    crew.Value.SecondHelper,
                    req.StopIds,
                    req.TransferredAt.ToUniversalTime(),
                    req.Reason,
                    req.Place,
                    req.IncidentId),
                currentUser.GetUserName());
        }
        catch (InvalidOperationException ex)
        {
            return Result<TripRescueDto>.Fail(ex.Message);
        }

        await tenantUow.Repository<TripRescue>().AddAsync(rescue, ct);
        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded rescue {RescueId} of trip {TripNumber} by truck {TruckNumber} ({DropCount} drops)",
            rescue.Id, trip.Number, truck.Number, req.StopIds.Count);

        var dtos = await TripRescueReader.ForTripAsync(tenantUow, trip.Id, ct);
        return Result<TripRescueDto>.Ok(dtos.First(r => r.Id == rescue.Id));
    }
}
