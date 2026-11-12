using System.Collections.Concurrent;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Communications.SignalR.Hubs;

public class TrackingHubContext
{
    /// <summary>
    ///     How often a connected phone's position is written to the truck record. Live viewers get
    ///     every update over SignalR; the database copy only has to be fresh enough for pages that
    ///     load it (trip details, truck pages, a map opened mid-shift).
    /// </summary>
    public static readonly TimeSpan PersistInterval = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, ClientState> connectedClients = new();

    public void AddClient(string connectionId, TruckGeolocationDto? geolocationData)
    {
        connectedClients.TryAdd(connectionId, new ClientState(geolocationData, null));
    }

    public TruckGeolocationDto? GetGeolocationData(string connectionId)
    {
        connectedClients.TryGetValue(connectionId, out var state);
        return state?.Geolocation;
    }

    public void UpdateGeolocationData(string connectionId, TruckGeolocationDto truckGeolocationDto)
    {
        connectedClients.AddOrUpdate(
            connectionId,
            _ => new ClientState(truckGeolocationDto, null),
            (_, state) => state with { Geolocation = truckGeolocationDto });
    }

    /// <summary>
    ///     True when this connection's position has not been saved within <see cref="PersistInterval" />.
    ///     Claims the slot, so concurrent updates from the same connection save once.
    /// </summary>
    public bool TryClaimPersist(string connectionId, DateTime utcNow)
    {
        while (connectedClients.TryGetValue(connectionId, out var state))
        {
            if (state.LastPersistedAt is { } last && utcNow - last < PersistInterval)
            {
                return false;
            }

            if (connectedClients.TryUpdate(connectionId, state with { LastPersistedAt = utcNow }, state))
            {
                return true;
            }
        }

        return false;
    }

    public void RemoveClient(string connectionId)
    {
        connectedClients.TryRemove(connectionId, out _);
    }

    private sealed record ClientState(TruckGeolocationDto? Geolocation, DateTime? LastPersistedAt);
}
