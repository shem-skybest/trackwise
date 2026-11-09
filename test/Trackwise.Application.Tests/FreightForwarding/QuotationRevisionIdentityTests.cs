using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class QuotationRevisionIdentityTests
{
    [Fact]
    public void UpdateDetails_DifferentCustomer_IsRejected()
    {
        var revision = CreateRevision();

        var exception = Assert.Throws<InvalidOperationException>(() => Update(revision, customerId: Guid.NewGuid()));

        Assert.Contains("same customer", exception.Message);
    }

    [Fact]
    public void UpdateDetails_DifferentFreightMode_IsRejected()
    {
        var revision = CreateRevision();

        var exception = Assert.Throws<InvalidOperationException>(() => Update(revision, freightMode: FreightMode.Import));

        Assert.Contains("same customer, freight mode, and route", exception.Message);
    }

    [Fact]
    public void UpdateDetails_DifferentRoute_IsRejected()
    {
        var revision = CreateRevision();
        var differentRoute = LaneTransit.PointToPoint(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            150m);

        var exception = Assert.Throws<InvalidOperationException>(() => Update(revision, lane: differentRoute));

        Assert.Contains("same customer, freight mode, and route", exception.Message);
    }

    [Fact]
    public void UpdateDetails_SameRouteWithChangedWindows_IsAllowed()
    {
        var revision = CreateRevision();
        var current = revision.Lane.Stops;
        var sameRoute = new LaneTransit(
            [
                RouteStop.Create(1, current[0].LocationId, StopType.Pickup, DateTimeOffset.UtcNow.AddHours(2)),
                RouteStop.Create(2, current[1].LocationId, StopType.Dropoff, null, DateTimeOffset.UtcNow.AddDays(2))
            ],
            120m);

        Update(revision, lane: sameRoute, notes: "Updated delivery window");

        Assert.Equal("Updated delivery window", revision.Notes);
    }

    private static Quotation CreateRevision()
    {
        var quotation = Quotation.Create(
            Guid.NewGuid(),
            branchId: Guid.NewGuid(),
            "QT-2026-DOM-00001",
            "USD",
            TransportMode.Road,
            FreightMode.Domestic,
            ShipmentType.FTL,
            Incoterm.DAP,
            LaneTransit.PointToPoint(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(1),
                100m),
            null);
        typeof(Quotation).GetProperty(nameof(Quotation.Status))!
            .SetValue(quotation, QuotationStatus.Accepted);
        return quotation.CreateRevision("QT-2026-DOM-00002", 2);
    }

    private static void Update(
        Quotation quotation,
        Guid? customerId = null,
        FreightMode? freightMode = null,
        LaneTransit? lane = null,
        string? notes = null)
    {
        quotation.UpdateDetails(
            customerId ?? quotation.CustomerId,
            quotation.BranchId,
            quotation.Currency,
            quotation.Mode,
            freightMode ?? quotation.FreightMode,
            quotation.ShipmentType,
            quotation.Incoterm,
            lane ?? quotation.Lane,
            null,
            notes: notes);
    }
}
