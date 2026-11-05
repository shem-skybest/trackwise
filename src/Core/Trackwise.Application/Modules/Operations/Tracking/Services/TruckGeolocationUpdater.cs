using Trackwise.Application.Abstractions.Realtime;
using Trackwise.Shared.Models;
using MediatR;
using Trackwise.Application.Modules.Operations.Trucks.Commands;

namespace Trackwise.Application.Modules.Operations.Tracking.Services;

internal sealed class TruckGeolocationUpdater(IMediator mediator) : ITruckGeolocationUpdater
{
    public Task UpdateAsync(TruckGeolocationDto geolocation, CancellationToken ct = default)
        => mediator.Send(new SetTruckGeolocationCommand(geolocation), ct);
}
