using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class LoadProximityTests
{
    private static Load DispatchedLoad()
    {
        var load = new Load
        {
            Name = "Load 1",
            Type = LoadType.GeneralFreight,
            Customer = null!,
            OriginAddress = new Address
            {
                StreetAddress = "1 Origin",
                CityOrMunicipality = "City",
                Province = "ST",
                Region = "",
                Zipcode = "00000",
                Country = "US"
            },
            OriginLocation = new GeoPoint(0, 0),
            DestinationAddress = new Address
            {
                StreetAddress = "1 Dest",
                CityOrMunicipality = "City",
                Province = "ST",
                Region = "",
                Zipcode = "00000",
                Country = "US"
            },
            DestinationLocation = new GeoPoint(0, 0),
            DeliveryCost = Money.Zero("USD")
        };
        load.UpdateStatus(LoadStatus.Dispatched);
        return load;
    }

    [Fact]
    public void ConfirmPickup_AtTheWarehouse_DoesNotUnlockDelivery()
    {
        var load = DispatchedLoad();
        load.UpdateProximity(true);
        Assert.True(load.CanConfirmPickUp);

        load.UpdateStatus(LoadStatus.PickedUp, force: true);

        Assert.False(load.IsInProximity);
        Assert.False(load.CanConfirmDelivery);
    }

    [Fact]
    public void ConfirmPickup_ThenArrivingAtTheDrop_UnlocksDelivery()
    {
        var load = DispatchedLoad();
        load.UpdateProximity(true);
        load.UpdateStatus(LoadStatus.PickedUp, force: true);

        load.UpdateProximity(true);

        Assert.True(load.CanConfirmDelivery);
    }

    [Fact]
    public void UpdateStatus_SameStatus_KeepsProximity()
    {
        var load = DispatchedLoad();
        load.UpdateProximity(true);

        load.UpdateStatus(LoadStatus.Dispatched, force: true);

        Assert.True(load.IsInProximity);
    }
}
