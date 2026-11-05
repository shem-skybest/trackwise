using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class ContainerTimelineTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid DeliveryLegId = Guid.NewGuid();
    private static readonly Guid PickupLegId = Guid.NewGuid();

    private readonly WorkfileContainerDto box1 = new() { Id = Guid.NewGuid(), ContainerNumber = "MAEU7025097" };
    private readonly WorkfileContainerDto box2 = new() { Id = Guid.NewGuid(), ContainerTypeCode = "HC40" };

    private List<WorkfileContainerDto> Boxes => [box1, box2];

    private static VesselDates Import(DateTime? etd = null, DateTime? eta = null) =>
        new(etd, eta, "Shanghai", "Manila North Harbor");

    private static LegMove Delivery(
        Guid slotId,
        DateTime? planned = null,
        DateTime? pickupArrived = null,
        DateTime? pickupDeparted = null,
        DateTime? dropArrived = null,
        DateTime? dropDeparted = null,
        long? trip = 70) =>
        new(DeliveryLegId, "SLC-TRK0001-26-M", TruckLegRole.Delivery, slotId, trip,
            "MICT", true, "Northgate Taytay", false,
            planned, pickupArrived, pickupDeparted, dropArrived, dropDeparted);

    private static LegMove Pickup(Guid slotId, DateTime? dropDeparted) =>
        new(PickupLegId, "SLC-TRK0002-26-M", TruckLegRole.Pickup, slotId, 71,
            "Northgate Taytay", false, "MICT", true,
            null, Now.AddDays(-3), Now.AddDays(-3).AddHours(1), Now.AddDays(-3).AddHours(3), dropDeparted);

    #region Container status

    [Fact]
    public void Build_NothingHappened_LeavesBoxesNotStarted()
    {
        ContainerTimeline.Build(Import(eta: Now.AddDays(5)), [], Boxes, Now);

        Assert.All(Boxes, b => Assert.Equal(ContainerMoveStatus.NotStarted, b.MoveStatus));
        Assert.All(Boxes, b => Assert.Null(b.MoveStatusSince));
    }

    [Fact]
    public void Build_SailedNotArrived_IsOnVesselHeadingToDestination()
    {
        ContainerTimeline.Build(Import(Now.AddDays(-2), Now.AddDays(3)), [], Boxes, Now);

        Assert.Equal(ContainerMoveStatus.OnVessel, box1.MoveStatus);
        Assert.Equal("Manila North Harbor", box1.MoveStatusPlace);
        Assert.Equal(Now.AddDays(-2), box1.MoveStatusSince);
    }

    [Fact]
    public void Build_EtaPassedNoTruckYet_IsAtPort()
    {
        ContainerTimeline.Build(Import(Now.AddDays(-9), Now.AddDays(-1)), [], Boxes, Now);

        Assert.Equal(ContainerMoveStatus.AtPort, box1.MoveStatus);
        Assert.Equal(Now.AddDays(-1), box1.MoveStatusSince);
    }

    [Fact]
    public void Build_TruckLeftThePort_IsOnTheRoadToTheDoor()
    {
        var move = Delivery(box1.Id, pickupArrived: Now.AddHours(-3), pickupDeparted: Now.AddHours(-2));

        ContainerTimeline.Build(Import(eta: Now.AddDays(-1)), [move], Boxes, Now);

        Assert.Equal(ContainerMoveStatus.OnTheRoad, box1.MoveStatus);
        Assert.Equal("Northgate Taytay", box1.MoveStatusPlace);
        Assert.Equal(ContainerMoveStatus.AtPort, box2.MoveStatus);
    }

    [Fact]
    public void Build_DeliveryLegLeftTheDoor_IsDelivered()
    {
        var move = Delivery(box1.Id, Now.AddDays(-1), Now.AddHours(-6), Now.AddHours(-5), Now.AddHours(-3), Now.AddHours(-1));

        ContainerTimeline.Build(Import(eta: Now.AddDays(-2)), [move], Boxes, Now);

        Assert.Equal(ContainerMoveStatus.Delivered, box1.MoveStatus);
        Assert.Equal(Now.AddHours(-1), box1.MoveStatusSince);
    }

    [Fact]
    public void Build_StaleEtaAfterTheDelivery_DoesNotPutTheBoxBackAtPort()
    {
        var move = Delivery(box1.Id, null, Now.AddHours(-6), Now.AddHours(-5), Now.AddHours(-3), Now.AddHours(-2));

        ContainerTimeline.Build(Import(eta: Now.AddHours(-1)), [move], Boxes, Now);

        Assert.Equal(ContainerMoveStatus.Delivered, box1.MoveStatus);
    }

    [Fact]
    public void Build_ExportDroppedAtPortThenSailed_IsOnVessel()
    {
        var vessel = new VesselDates(Now.AddDays(-1), Now.AddDays(6), "Manila", "Busan");

        ContainerTimeline.Build(vessel, [Pickup(box1.Id, Now.AddDays(-3).AddHours(4))], Boxes, Now);

        Assert.Equal(ContainerMoveStatus.OnVessel, box1.MoveStatus);
        Assert.Equal("Busan", box1.MoveStatusPlace);
    }

    [Fact]
    public void Build_ExportDroppedAtPortBeforeEtd_IsAtPort()
    {
        var vessel = new VesselDates(Now.AddDays(2), Now.AddDays(9), "Manila", "Busan");

        ContainerTimeline.Build(vessel, [Pickup(box1.Id, Now.AddDays(-3).AddHours(4))], Boxes, Now);

        Assert.Equal(ContainerMoveStatus.AtPort, box1.MoveStatus);
        Assert.Equal("MICT", box1.MoveStatusPlace);
    }

    [Fact]
    public void Build_TransferFromPortToYard_IsAtYard()
    {
        var transfer = Delivery(box1.Id, null, Now.AddHours(-6), Now.AddHours(-5), Now.AddHours(-3), Now.AddHours(-2))
            with { Role = TruckLegRole.Transfer };

        ContainerTimeline.Build(Import(eta: Now.AddDays(-1)), [transfer], Boxes, Now);

        Assert.Equal(ContainerMoveStatus.AtYard, box1.MoveStatus);
    }

    #endregion

    #region Timeline entries

    [Fact]
    public void Build_BoxesOnTheSameTruckEvent_ShareOneEntry()
    {
        var arrived = Now.AddHours(-3);
        var moves = new[] { Delivery(box1.Id, pickupArrived: arrived), Delivery(box2.Id, pickupArrived: arrived) };

        var timeline = ContainerTimeline.Build(Import(), moves, Boxes, Now);

        var entry = Assert.Single(timeline);
        Assert.Equal(WorkfileTimelineEventKind.TruckArrivedAtPickup, entry.Kind);
        Assert.Equal(["MAEU7025097", "HC40 #2"], entry.Containers);
        Assert.Equal("SLC-TRK0001-26-M", entry.LegReferenceNumber);
        Assert.Equal(70, entry.TripNumber);
    }

    [Fact]
    public void Build_ImportMove_ListsVesselBeforeTheDeliveryLegAndMarksWhatIsAhead()
    {
        var move = Delivery(box1.Id, planned: Now.AddDays(1), trip: null);

        var timeline = ContainerTimeline.Build(Import(Now.AddDays(-6), Now.AddHours(-1)), [move], Boxes, Now);

        Assert.Equal(
            [WorkfileTimelineEventKind.VesselDeparted, WorkfileTimelineEventKind.VesselArrived, WorkfileTimelineEventKind.PickupPlanned],
            timeline.Select(e => e.Kind));
        Assert.Equal([false, false, true], timeline.Select(e => e.IsUpcoming));
        Assert.All(timeline, e => Assert.True(e.IsEstimate));
    }

    [Fact]
    public void Build_PlannedPickup_DropsOffOnceTheTruckArrives()
    {
        var move = Delivery(box1.Id, planned: Now.AddHours(-4), pickupArrived: Now.AddHours(-3));

        var timeline = ContainerTimeline.Build(Import(), [move], Boxes, Now);

        Assert.DoesNotContain(timeline, e => e.Kind == WorkfileTimelineEventKind.PickupPlanned);
    }

    #endregion
}
