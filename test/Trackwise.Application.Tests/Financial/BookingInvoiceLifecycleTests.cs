using Trackwise.Application.Modules.Financial.Invoices.Commands.BookingInvoiceCommands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Financial;

public sealed class BookingInvoiceLifecycleTests
{
    [Theory]
    [InlineData(InvoiceStatus.Draft, true)]
    [InlineData(InvoiceStatus.PendingApproval, true)]
    [InlineData(InvoiceStatus.Rejected, true)]
    [InlineData(InvoiceStatus.Approved, false)]
    [InlineData(InvoiceStatus.Issued, false)]
    [InlineData(InvoiceStatus.Sent, false)]
    [InlineData(InvoiceStatus.PartiallyPaid, false)]
    [InlineData(InvoiceStatus.Overdue, false)]
    [InlineData(InvoiceStatus.Paid, false)]
    [InlineData(InvoiceStatus.Cancelled, false)]
    public void CanMove_ToCancelled_OnlyBeforeApproval(InvoiceStatus current, bool expected)
    {
        var canCancel = BookingInvoiceLifecycle.CanMove(current, InvoiceStatus.Cancelled);

        Assert.Equal(expected, canCancel);
    }

    [Fact]
    public void ReleaseChargesFromInvoice_ReleasesOnlyThatInvoicesCharges_AndLeavesBookingStatusAlone()
    {
        var booking = CreateBooking();
        var released = booking.AddAdditionalCharge(
            ChargeCategory.Accessorial, "Detention", 1m, 100m, 150m, "Late pickup");
        var retained = booking.AddAdditionalCharge(
            ChargeCategory.Accessorial, "Extra handling", 1m, 50m, 75m, "Customer request");
        var cancelledInvoiceId = Guid.NewGuid();
        var activeInvoiceId = Guid.NewGuid();

        booking.MarkChargesInvoiced(cancelledInvoiceId, [released.Id]);
        booking.MarkChargesInvoiced(activeInvoiceId, [retained.Id]);

        booking.ReleaseChargesFromInvoice(cancelledInvoiceId);

        Assert.Equal(BillingStatus.Unbilled, released.BillingStatus);
        Assert.Null(released.InvoiceId);
        Assert.Equal(BillingStatus.Invoiced, retained.BillingStatus);
        Assert.Equal(activeInvoiceId, retained.InvoiceId);
        Assert.Equal(BookingStatus.Draft, booking.Status);
    }

    private static Booking CreateBooking()
    {
        var lane = LaneTransit.PointToPoint(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            100m);
        var cargo = CargoSpecification.Create(
            "General Cargo",
            1000m,
            2,
            new Money { Amount = 5000m, Currency = "USD" },
            false);
        var quotation = Quotation.Create(
            Guid.NewGuid(),
            "QT-TEST-BILL-001",
            "USD",
            TransportMode.Road,
            FreightMode.Domestic,
            ShipmentType.FTL,
            Incoterm.DAP,
            lane,
            cargo);

        var chargeCode = ChargeCode.Create(
            "FRT", "Freight", ChargeCategory.Base, [FreightMode.Domestic], "km");
        chargeCode.Currency = "USD";
        var tariffRate = new TariffRate
        {
            ChargeCode = chargeCode,
            PricingModel = PricingModel.PerUnit,
            BaseAmount = 10m,
            Unit = "km"
        };
        var line = quotation.AddLine(
            tariffRate.Id,
            ChargeCategory.Base,
            OperationalServiceType.LineHaul,
            100m,
            MarkupType.Percentage,
            10m,
            null,
            null);
        typeof(QuotationLine).GetProperty(nameof(QuotationLine.TariffRate))?.SetValue(line, tariffRate);
        quotation.Approve("billing-test");
        quotation.Issue(TimeSpan.FromDays(30));
        quotation.Accept();
        return Booking.CreateDraftFromQuotation(quotation);
    }
}
