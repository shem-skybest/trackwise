using Trackwise.Application.Abstractions;
using Trackwise.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Events;

internal sealed class NewTripCreatedHandler(ILogger<NewTripCreatedHandler> logger)
    : IDomainEventHandler<NewTripCreatedEvent>
{
    public Task Handle(NewTripCreatedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Created a new trip {Id}", @event.TripId);
        return Task.CompletedTask;
    }
}
