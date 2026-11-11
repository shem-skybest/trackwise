using Trackwise.Application.Abstractions.Realtime;
using Trackwise.Infrastructure.Communications.SignalR.Clients;
using Trackwise.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Trackwise.Infrastructure.Communications.SignalR.Hubs;

public class TrackingHub(
    ITruckGeolocationUpdater geolocationUpdater,
    TrackingHubContext hubContext,
    ILogger<TrackingHub> logger) : Hub<ITrackingHubClient>
{
    private const string TripGroupPrefix = "trip:";

    public override Task OnConnectedAsync()
    {
        hubContext.AddClient(Context.ConnectionId, null);
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var geolocationData = hubContext.GetGeolocationData(Context.ConnectionId);

        if (geolocationData != null && geolocationData.TruckId != Guid.Empty)
        {
            await geolocationUpdater.UpdateAsync(geolocationData);
        }

        hubContext.RemoveClient(Context.ConnectionId);
    }

    #region Geolocation Methods

    public async Task SendGeolocationData(TruckGeolocationDto truckGeolocation)
    {
        await Clients
            .Group(truckGeolocation.TenantId.ToString())
            .ReceiveGeolocationData(truckGeolocation);
        hubContext.UpdateGeolocationData(Context.ConnectionId, truckGeolocation);

        // Pages that load the saved location (trip details, a map opened mid-shift) never see the
        // broadcast above, so the truck record has to keep up while the phone stays connected.
        if (truckGeolocation.TruckId != Guid.Empty
            && hubContext.TryClaimPersist(Context.ConnectionId, DateTime.UtcNow))
        {
            try
            {
                await geolocationUpdater.UpdateAsync(truckGeolocation, Context.ConnectionAborted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to save live geolocation for truck {TruckId}", truckGeolocation.TruckId);
            }
        }
    }

    #endregion

    #region Tenant Subscription

    public async Task RegisterTenant(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
    }

    public async Task UnregisterTenant(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
    }

    #endregion

    #region Trip Subscription

    /// <summary>
    ///     Subscribe to updates for a specific trip.
    /// </summary>
    public async Task SubscribeToTrip(string tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{TripGroupPrefix}{tripId}");
    }

    /// <summary>
    ///     Unsubscribe from updates for a specific trip.
    /// </summary>
    public async Task UnsubscribeFromTrip(string tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{TripGroupPrefix}{tripId}");
    }

    #endregion
}
