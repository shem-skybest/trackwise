using Trackwise.Domain.Events;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Status transition methods for Trip entity.
/// </summary>
public partial class Trip
{
    /// <summary>
    /// Dispatches the trip and its draft loads. The driver app acts per load and only offers
    /// pickup and delivery on a dispatched one. The loads' invoices are left as they are, unlike
    /// <see cref="Load.Dispatch" />: a Cold Chain drop is billed in a batch, not on its own.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if trip is not in Draft status.</exception>
    public void Dispatch()
    {
        if (!TripStatusMachine.CanDispatch(Status))
        {
            throw new InvalidOperationException("Trip already dispatched");
        }

        Status = TripStatus.Dispatched;
        DispatchedAt = DateTime.UtcNow;
        DispatchDraftLoads(GetLoads(), DispatchedAt.Value);
        DomainEvents.Add(new TripDispatchedEvent(Id));
    }

    private static void DispatchDraftLoads(IEnumerable<Load> loads, DateTime at)
    {
        foreach (var load in loads.Where(l => l.Status == LoadStatus.Draft))
        {
            load.UpdateStatus(LoadStatus.Dispatched, at: at);
        }
    }

    /// <summary>
    /// Cancels the trip and all associated loads.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if trip is already completed.</exception>
    public void Cancel()
    {
        if (!TripStatusMachine.CanCancel(Status))
        {
            throw new InvalidOperationException("Cannot cancel a completed trip");
        }

        var reached = Stops
            .Where(s => s.Type == TripStopType.DropOff)
            .FirstOrDefault(s => s.Load.Status != LoadStatus.Cancelled && LoadStatusMachine.IsResolved(s.Load.Status));
        if (reached is not null)
        {
            throw new InvalidOperationException(
                $"Drop {DropNumber(reached)} is already {reached.Load.Status.GetDescription().ToLowerInvariant()}, " +
                "so the trip ran - record the remaining drops' outcomes (or a rescue) instead of cancelling it.");
        }

        if (Status is TripStatus.Dispatched or TripStatus.InTransit)
        {
            DomainEvents.Add(new TripCancelledEvent(Id, DriverId));
        }

        Status = TripStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        foreach (var stop in Stops)
        {
            stop.ArrivedAt = null;
            stop.Load.Cancel();
        }
    }

    /// <summary>
    /// Refreshes the trip status based on the current state of stops. A trip completes once every
    /// drop-off has departed with a terminal outcome - delivered or not. Waiting for the departure,
    /// not just the arrival, is what leaves room to record the last drop's real outcome.
    /// </summary>
    private void RefreshStatus()
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
        {
            return;
        }

        var dropOffs = Stops.Where(s => s.Type == TripStopType.DropOff).ToList();
        var allDropOffsResolved = dropOffs.Count > 0 && dropOffs.All(s =>
            s.DepartedAt.HasValue && LoadStatusMachine.IsResolved(s.Load.Status));

        var anyPickupDone = Stops
            .Any(s => s is { Type: TripStopType.PickUp, Load.Status: LoadStatus.PickedUp });

        if (allDropOffsResolved)
        {
            Status = TripStatus.Completed;
            CompletedAt = dropOffs.Max(s => s.DepartedAt!.Value);
            DomainEvents.Add(new TripCompletedEvent(Id));
        }
        else if (anyPickupDone)
        {
            Status = TripStatus.InTransit;
        }
    }
}
