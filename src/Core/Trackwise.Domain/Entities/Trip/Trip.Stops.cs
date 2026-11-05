using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Stop management methods for Trip entity.
/// </summary>
public partial class Trip
{
    /// <summary>
    /// Gets all unique loads associated with the trip.
    /// </summary>
    public IReadOnlyList<Load> GetLoads()
    {
        return Stops
            .Select(s => s.Load)
            .Where(i => i is not null)
            .Distinct(new LoadComparer())
            .ToArray();
    }

    /// <summary>
    /// Adds new loads to the trip, creating corresponding pick-up and drop-off stops.
    /// </summary>
    /// <param name="loads">The collection of loads to be added.</param>
    /// <exception cref="InvalidOperationException">Thrown if the trip status is not Draft.</exception>
    public void AddLoads(IEnumerable<Load> loads)
    {
        if (!TripStatusMachine.CanModify(Status))
        {
            throw new InvalidOperationException("Cannot modify loads unless trip is Draft.");
        }

        var added = loads.ToList();
        TripFactory.AddStopsForLoads(this, added);

        if (Status == TripStatus.Dispatched)
        {
            DispatchDraftLoads(added, DateTime.UtcNow);
        }

        TotalDistance = GetLoads().Sum(l => l.Distance);
        MarkPriceOutdated();
    }

    /// <summary>
    /// Removes a load from the trip.
    /// </summary>
    /// <param name="loadId">The ID of the load to be removed.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the trip status is not Draft or Dispatched, or the load's pickup or drop has a
    /// recorded arrival - a drop that didn't happen is recorded with its outcome instead.
    /// </exception>
    public void RemoveLoad(Guid loadId)
    {
        if (!TripStatusMachine.CanModify(Status))
        {
            throw new InvalidOperationException("Cannot modify loads unless trip is Draft.");
        }

        var toRemove = Stops.Where(s => s.LoadId == loadId).ToList();
        if (toRemove.Count == 0)
        {
            return;
        }

        if (toRemove.Any(s => s.ArrivedAt.HasValue))
        {
            throw new InvalidOperationException(
                "This drop already has a recorded arrival, so it can't be removed. If it didn't happen, " +
                "record its outcome (foul trip, cancelled by client) on the crew report.");
        }

        foreach (var s in toRemove)
        {
            Stops.Remove(s);
        }

        RenumberStops();
        TotalDistance = GetLoads().Sum(l => l.Distance);
        MarkPriceOutdated();
    }

    /// <summary>Whether any stop has a recorded arrival - the point where the trip's plan becomes its record.</summary>
    public bool HasRecordedStops => Stops.Any(s => s.ArrivedAt.HasValue);

    private void EnsureNoStopRecorded(string what)
    {
        if (HasRecordedStops)
        {
            throw new InvalidOperationException(
                $"{what} can't change once a stop's arrival is recorded - the trip is the record of who ran it.");
        }
    }

    /// <summary>
    /// Renumbers all stops in sequential order.
    /// </summary>
    private void RenumberStops()
    {
        var order = 1;
        foreach (var s in Stops.OrderBy(x => x.Order))
        {
            s.Order = order++;
        }
    }
}
