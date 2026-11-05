using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// A truck leg is a child trucking workfile of a forwarding file: its own SLC-TRK number, the
/// parent's client, and no booking or bill of its own (FORWARDING_LEGS_PLAN.md F1, F2, F6).
/// </summary>
public class TruckLegTests
{
    private static readonly WorkfileHeaderData Header = new() { ReportCategory = ReportCategory.Psb };

    private static RegularWorkfile Ocean(FreightMode direction) =>
        RegularWorkfile.Create("BKN-IMP-000001", direction, Guid.NewGuid(), null, Header);

    [Theory]
    [InlineData(FreightMode.Import)]
    [InlineData(FreightMode.Export)]
    [InlineData(FreightMode.Domestic)]
    public void CreateLeg_OceanFileWithAPhilippineEnd_LinksToParentAndTakesItsClient(FreightMode direction)
    {
        var parent = Ocean(direction);

        var leg = TruckingWorkfile.CreateLeg("SLC-TRK0001-26-M", parent, TruckLegRole.Delivery, new WorkfileHeaderData());

        Assert.True(leg.IsLeg);
        Assert.Equal(parent.Id, leg.ParentWorkfileId);
        Assert.Equal(TruckLegRole.Delivery, leg.LegRole);
        Assert.Equal(parent.CustomerId, leg.CustomerId);
        Assert.Null(leg.BookingId);
        Assert.Equal(WorkfileStatus.Open, leg.Status);
    }

    [Fact]
    public void CreateLeg_DrawsFromTheSlcTrkSeries_NotBknTrk()
    {
        var leg = TruckingWorkfile.CreateLeg("SLC-TRK0001-26-M", Ocean(FreightMode.Import), TruckLegRole.Pickup, new WorkfileHeaderData());
        var standalone = TruckingWorkfile.Create("BKN-TRK-000001", TruckingVariant.Truck, Guid.NewGuid(), null, Header);

        Assert.Equal("SLC-TRK", leg.ReferenceSeries);
        Assert.Equal("BKN-TRK", standalone.ReferenceSeries);
        Assert.False(standalone.IsLeg);
    }

    [Fact]
    public void CreateLeg_Triangular_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TruckingWorkfile.CreateLeg("SLC-TRK0001-26-M", Ocean(FreightMode.Triangular), TruckLegRole.Delivery, new WorkfileHeaderData()));
    }

    [Fact]
    public void CreateLeg_UnderATruckingFile_Throws()
    {
        var trucking = TruckingWorkfile.Create("BKN-TRK-000001", TruckingVariant.Truck, Guid.NewGuid(), null, Header);

        Assert.Throws<InvalidOperationException>(() =>
            TruckingWorkfile.CreateLeg("SLC-TRK0001-26-M", trucking, TruckLegRole.Delivery, new WorkfileHeaderData()));
    }

    [Fact]
    public void CreateLeg_ParentNoLongerOpen_Throws()
    {
        var parent = Ocean(FreightMode.Import);
        parent.SubmitForApproval();

        Assert.Throws<InvalidOperationException>(() =>
            TruckingWorkfile.CreateLeg("SLC-TRK0001-26-M", parent, TruckLegRole.Delivery, new WorkfileHeaderData()));
    }
}
