using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class CorrectTripGarageTimesHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    ILogger<CorrectTripGarageTimesHandler> logger)
    : IAppRequestHandler<CorrectTripGarageTimesCommand, Result>
{
    public async Task<Result> Handle(CorrectTripGarageTimesCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        IReadOnlyList<TripCorrection> changes;
        try
        {
            changes = trip.CorrectGarageTimes(
                req.DepartedAt?.ToUniversalTime(),
                req.ArrivedAt?.ToUniversalTime(),
                req.Reason,
                currentUser.GetUserName());
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await TripCorrectionWriter.AddAsync(tenantUow, changes, ct);
        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation("Corrected garage times for trip '{TripName}' (ID: {TripId})", trip.Name, trip.Id);
        return Result.Ok();
    }
}
