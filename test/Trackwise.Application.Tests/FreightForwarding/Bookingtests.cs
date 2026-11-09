using Trackwise.Application.Tests.TestKit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// Domain rules of the unified Booking aggregate (quotation-derived bookings).
/// Handler behaviour lives in <see cref="BookingStatusHandlersTests"/>.
/// </summary>
public class BookingTests
{
    private const string QuotationReference = "QT-26-DOMMNL-00001";

    #region Numbering

    [Theory]
    [InlineData("QT-26-DOMMNL-00001", 1, "BK-26-DOMMNL-00001-01")]
    [InlineData("QT-26-DOMMNL-00001", 2, "BK-26-DOMMNL-00001-02")]
    [InlineData("QT-26-IMPCEB-00042", 12, "BK-26-IMPCEB-00042-12")]
    [InlineData("qt-26-dommnl-00001", 1, "BK-26-DOMMNL-00001-01")]
    [InlineData("  QT-26-DOMMNL-00001  ", 1, "BK-26-DOMMNL-00001-01")]
    public void FromQuotationReference_DerivesBookingNumber(string reference, int sequence, string expected)
    {
        Assert.Equal(expected, BookingNumbering.FromQuotationReference(reference, sequence));
    }

    [Fact]
    public void FromQuotationReference_SequenceBeyondTwoDigits_SimplyWidens()
    {
        Assert.Equal("BK-26-DOMMNL-00001-100", BookingNumbering.FromQuotationReference(QuotationReference, 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FromQuotationReference_SequenceBelowOne_Throws(int sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BookingNumbering.FromQuotationReference(QuotationReference, sequence));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromQuotationReference_BlankReference_Throws(string reference)
    {
        Assert.ThrowsAny<ArgumentException>(() => BookingNumbering.FromQuotationReference(reference, 1));
    }

    [Fact]
    public void FromQuotationReference_WithoutQuotationPrefix_Throws()
    {
        Assert.Throws<ArgumentException>(() => BookingNumbering.FromQuotationReference("INV-26-0001", 1));
    }

    [Fact]
    public void FromBookingId_EmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(() => BookingNumbering.FromBookingId(Guid.Empty));
    }

    #endregion

    #region Creation

    [Fact]
    public void CreateDraftFromQuotation_AcceptedQuotation_StartsDraftWithDerivedNumber()
    {
        var quotation = AcceptedQuotation();

        var booking = Booking.CreateDraftFromQuotation(quotation, 1);

        Assert.Equal(BookingStatus.Draft, booking.Status);
        Assert.Equal("BK-26-DOMMNL-00001-01", booking.BookingNumber);
        Assert.Equal(quotation.Id, booking.QuotationId);
        Assert.Equal(quotation.ReferenceNumber, booking.QuotationReference);
        Assert.Equal(quotation.CustomerId, booking.CustomerId);
        Assert.Equal(quotation.Currency, booking.Currency);
        Assert.Equal(quotation.FreightMode, booking.FreightMode);
        Assert.False(booking.IsFromRateAgreement);
    }

    [Fact]
    public void CreateDraftFromQuotation_SecondBooking_GetsNextSequenceSuffix()
    {
        var quotation = AcceptedQuotation();

        var first = Booking.CreateDraftFromQuotation(quotation, 1);
        var second = Booking.CreateDraftFromQuotation(quotation, 2);

        Assert.EndsWith("-01", first.BookingNumber);
        Assert.EndsWith("-02", second.BookingNumber);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void CreateDraftFromQuotation_CopiesOneChargePerQuotationLine()
    {
        var quotation = AcceptedQuotation();

        var booking = Booking.CreateDraftFromQuotation(quotation, 1);

        Assert.Equal(quotation.Lines.Count, booking.Charges.Count);
        Assert.All(booking.Charges, c =>
        {
            Assert.Equal(BookingChargeSource.Quoted, c.Source);
            Assert.Equal(booking.Id, c.BookingId);
            Assert.Equal(BillingStatus.Unbilled, c.BillingStatus);
        });
    }

    [Fact]
    public void CreateDraftFromQuotation_NonAcceptedQuotation_Throws()
    {
        var quotation = DraftQuotation();

        Assert.Throws<InvalidOperationException>(() => Booking.CreateDraftFromQuotation(quotation, 1));
    }

    [Fact]
    public void CreateDraftFromQuotation_CreditCheckRequired_StartsPending()
    {
        var booking = Booking.CreateDraftFromQuotation(AcceptedQuotation(), 1, customerCreditCheckRequired: true);

        Assert.Equal(CreditCheckStatus.Pending, booking.CreditCheckStatus);
    }

    [Fact]
    public void CreateDraftFromQuotation_CreditCheckNotRequired_StartsNotRequired()
    {
        var booking = Booking.CreateDraftFromQuotation(AcceptedQuotation(), 1);

        Assert.Equal(CreditCheckStatus.NotRequired, booking.CreditCheckStatus);
    }

    [Fact]
    public void CreateDraftFromQuotation_RequestedDates_OnlyMovePickupAndDropoffWindows()
    {
        var pickup = new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc);
        var delivery = new DateTime(2026, 11, 22, 17, 0, 0, DateTimeKind.Utc);

        var booking = Booking.CreateDraftFromQuotation(
            AcceptedQuotation(), 1, requestedPickupDate: pickup, requestedDeliveryDate: delivery);

        Assert.Equal(new DateTimeOffset(pickup), booking.RequestedPickupStart);
        Assert.Equal(new DateTimeOffset(delivery), booking.RequestedDeliveryEnd);
    }

    [Fact]
    public void CreateDraftFromQuotation_RaisesDraftCreatedEvent()
    {
        var quotation = AcceptedQuotation();

        var booking = Booking.CreateDraftFromQuotation(quotation, 1);

        var evt = Assert.Single(booking.DomainEvents.OfType<BookingDraftCreated>());
        Assert.Equal(booking.Id, evt.Id);
        Assert.Equal(quotation.Id, evt.QuotationId);
    }

    #endregion

    #region Confirmation blockers

    [Fact]
    public void GetConfirmationBlockers_ReadyBooking_IsEmpty()
    {
        var booking = NewDraft();

        Assert.Empty(booking.GetConfirmationBlockers());
    }

    [Fact]
    public void Confirm_PendingCreditCheck_Throws()
    {
        var booking = NewDraft(creditCheckRequired: true);

        var ex = Assert.Throws<InvalidOperationException>(() => booking.Confirm("ops"));

        Assert.Contains("Credit check is pending", ex.Message);
    }

    [Fact]
    public void Confirm_RejectedCreditCheck_Throws()
    {
        var booking = NewDraft(creditCheckRequired: true);
        booking.RejectCredit("credit", "Over limit");

        var ex = Assert.Throws<InvalidOperationException>(() => booking.Confirm("ops"));

        Assert.Contains("Credit check was rejected", ex.Message);
    }

    [Fact]
    public void Confirm_ApprovedCreditCheck_Succeeds()
    {
        var booking = NewDraft(creditCheckRequired: true);
        booking.ApproveCredit("credit", "OK");

        booking.Confirm("ops");

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal("credit", booking.CreditCheckedBy);
    }

    [Fact]
    public void Confirm_SubmittedButUnverifiedDocument_Throws()
    {
        var booking = NewDraft();
        booking.AddDocument(BookingDocumentType.Kyc, "KYC-1");

        var ex = Assert.Throws<InvalidOperationException>(() => booking.Confirm("ops"));

        Assert.Contains("Kyc", ex.Message);
    }

    [Fact]
    public void Confirm_VerifiedDocument_Succeeds()
    {
        var booking = NewDraft();
        var doc = booking.AddDocument(BookingDocumentType.Kyc, "KYC-1");
        booking.VerifyDocument(doc.Id, "ops");

        booking.Confirm("ops");

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Confirm_ExpiredVerifiedDocument_Throws()
    {
        var booking = NewDraft();
        var doc = booking.AddDocument(BookingDocumentType.ImportPermit, "IP-1", DateTimeOffset.UtcNow.AddDays(-1));
        booking.VerifyDocument(doc.Id, "ops");

        Assert.False(doc.IsSatisfied);
        Assert.Throws<InvalidOperationException>(() => booking.Confirm("ops"));
    }

    [Fact]
    public void Confirm_WaivedDocument_Succeeds()
    {
        var booking = NewDraft();
        var doc = booking.AddDocument(BookingDocumentType.PackingList);
        booking.WaiveDocument(doc.Id, "manager", "Not needed for this shipment");

        booking.Confirm("ops");

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Confirm_RejectedDocument_Throws()
    {
        var booking = NewDraft();
        var doc = booking.AddDocument(BookingDocumentType.CommercialInvoice);
        booking.RejectDocument(doc.Id, "ops", "Illegible");

        Assert.Throws<InvalidOperationException>(() => booking.Confirm("ops"));
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_Throws()
    {
        var booking = NewDraft();
        booking.Confirm("ops");

        Assert.Throws<InvalidOperationException>(() => booking.Confirm("ops"));
    }

    [Fact]
    public void Confirm_RaisesConfirmedEvent()
    {
        var booking = NewDraft();

        booking.Confirm("ops");

        var evt = Assert.Single(booking.DomainEvents.OfType<BookingConfirmed>());
        Assert.Equal(booking.BookingNumber, evt.BookingNumber);
        Assert.Equal("ops", booking.ConfirmedBy);
        Assert.NotNull(booking.ConfirmedAt);
    }

    #endregion

    #region Lifecycle guards

    [Fact]
    public void MarkInProgress_DraftBooking_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => NewDraft().MarkInProgress());
    }

    [Fact]
    public void MarkInProgress_CalledTwice_IsIdempotent()
    {
        var booking = NewDraft();
        booking.Confirm("ops");
        booking.MarkInProgress();

        booking.MarkInProgress();

        Assert.Equal(BookingStatus.InProgress, booking.Status);
        Assert.Single(booking.DomainEvents.OfType<BookingStarted>());
    }

    [Fact]
    public void Complete_OnlyFromInProgress()
    {
        var booking = NewDraft();
        booking.Confirm("ops");

        Assert.Throws<InvalidOperationException>(() => booking.Complete());

        booking.MarkInProgress();
        booking.Complete();

        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.NotNull(booking.CompletedAt);
    }

    [Fact]
    public void Cancel_ConfirmedBooking_FlagsWasConfirmed()
    {
        var booking = NewDraft();
        booking.Confirm("ops");

        booking.Cancel("ops", "Shipper withdrew");

        var evt = Assert.Single(booking.DomainEvents.OfType<BookingCancelled>());
        Assert.True(evt.WasConfirmed);
        Assert.Equal("Shipper withdrew", booking.CancellationReason);
        Assert.Equal("ops", booking.CancelledBy);
    }

    [Fact]
    public void Cancel_DraftBooking_FlagsNotConfirmed()
    {
        var booking = NewDraft();

        booking.Cancel("ops", "Duplicate");

        Assert.False(Assert.Single(booking.DomainEvents.OfType<BookingCancelled>()).WasConfirmed);
    }

    [Fact]
    public void Cancel_BlankReason_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => NewDraft().Cancel("ops", " "));
    }

    [Fact]
    public void Cancel_CompletedBooking_Throws()
    {
        var booking = NewDraft();
        booking.Confirm("ops");
        booking.MarkInProgress();
        booking.Complete();

        Assert.Throws<InvalidOperationException>(() => booking.Cancel("ops", "Too late"));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var booking = NewDraft();
        booking.Cancel("ops", "First");

        Assert.Throws<InvalidOperationException>(() => booking.Cancel("ops", "Second"));
    }

    #endregion

    #region Draft-only edits

    [Fact]
    public void UpdateHeader_Draft_AppliesValues()
    {
        var booking = NewDraft();

        booking.UpdateHeader("  PO-123  ", "Handle with care", 45, 20m);

        Assert.Equal("PO-123", booking.CustomerReference);
        Assert.Equal("Handle with care", booking.Notes);
        Assert.Equal(45, booking.PaymentTermsDays);
        Assert.Equal(20m, booking.DepositRequiredPercent);
    }

    [Fact]
    public void UpdateHeader_AfterConfirmation_Throws()
    {
        var booking = NewDraft();
        booking.Confirm("ops");

        Assert.Throws<InvalidOperationException>(() => booking.UpdateHeader(null, null, 30, null));
    }

    [Fact]
    public void UpdateHeader_NegativePaymentTerms_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewDraft().UpdateHeader(null, null, -1, null));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void UpdateHeader_DepositOutOfRange_Throws(double deposit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewDraft().UpdateHeader(null, null, 30, (decimal)deposit));
    }

    #endregion

    #region Charges

    [Fact]
    public void AddAdditionalCharge_StoresAmountsAsQuantityTimesUnit()
    {
        var booking = NewDraft();

        var charge = booking.AddAdditionalCharge(ChargeCategory.Accessorial, "Extra Accessorial", 2m, 100m, 150m, "Customer request");

        Assert.Equal(BookingChargeSource.Additional, charge.Source);
        Assert.Equal(200m, charge.BuyingAmount);
        Assert.Equal(300m, charge.SellingAmount);
        Assert.Contains(charge, booking.Charges);
        Assert.Single(booking.DomainEvents.OfType<BookingChargeAdded>());
    }

    [Fact]
    public void AddAdditionalCharge_Discount_IsStoredNegative()
    {
        var booking = NewDraft();

        var charge = booking.AddAdditionalCharge(ChargeCategory.Discount, "Goodwill", 1m, 50m, 80m, "Service recovery");

        Assert.Equal(-50m, charge.BuyingAmount);
        Assert.Equal(-80m, charge.SellingAmount);
    }

    [Fact]
    public void AddAdditionalCharge_BlankReason_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            NewDraft().AddAdditionalCharge(ChargeCategory.Accessorial, "Extra", 1m, 1m, 1m, " "));
    }

    [Fact]
    public void AddAdditionalCharge_ZeroQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NewDraft().AddAdditionalCharge(ChargeCategory.Accessorial, "Extra", 0m, 1m, 1m, "Reason"));
    }

    [Fact]
    public void AddAdditionalCharge_CompletedBooking_Throws()
    {
        var booking = NewDraft();
        booking.Confirm("ops");
        booking.MarkInProgress();
        booking.Complete();

        Assert.Throws<InvalidOperationException>(() =>
            booking.AddAdditionalCharge(ChargeCategory.Accessorial, "Late", 1m, 1m, 1m, "Reason"));
    }

    [Fact]
    public void RemoveAdditionalCharge_QuotedCharge_Throws()
    {
        var booking = NewDraft();
        var quoted = booking.Charges.First(c => c.Source == BookingChargeSource.Quoted);

        Assert.Throws<InvalidOperationException>(() => booking.RemoveAdditionalCharge(quoted.Id));
    }

    [Fact]
    public void RemoveAdditionalCharge_AdditionalCharge_RemovesIt()
    {
        var booking = NewDraft();
        var charge = booking.AddAdditionalCharge(ChargeCategory.Accessorial, "Extra", 1m, 10m, 15m, "Reason");

        booking.RemoveAdditionalCharge(charge.Id);

        Assert.DoesNotContain(charge, booking.Charges);
        Assert.Single(booking.DomainEvents.OfType<BookingChargeRemoved>());
    }

    [Fact]
    public void RemoveAdditionalCharge_UnknownId_IsNoOp()
    {
        var booking = NewDraft();
        var before = booking.Charges.Count;

        booking.RemoveAdditionalCharge(Guid.NewGuid());

        Assert.Equal(before, booking.Charges.Count);
    }

    [Fact]
    public void RemoveAdditionalCharge_Invoiced_ThrowsUnlessAllowed()
    {
        var booking = NewDraft();
        var charge = booking.AddAdditionalCharge(ChargeCategory.Accessorial, "Extra", 1m, 10m, 15m, "Reason");
        booking.MarkChargesInvoiced(Guid.NewGuid(), [charge.Id]);

        Assert.Throws<InvalidOperationException>(() => booking.RemoveAdditionalCharge(charge.Id));

        booking.RemoveAdditionalCharge(charge.Id, allowInvoicedDraft: true);
        Assert.DoesNotContain(charge, booking.Charges);
    }

    [Fact]
    public void UpdateAdditionalCharge_QuotedCharge_Throws()
    {
        var booking = NewDraft();
        var quoted = booking.Charges.First(c => c.Source == BookingChargeSource.Quoted);

        Assert.Throws<InvalidOperationException>(() =>
            booking.UpdateAdditionalCharge(quoted.Id, "Changed", 1m, 1m, 1m, "Reason", allowInvoicedDraft: false));
    }

    [Fact]
    public void UpdateAdditionalCharge_Additional_RecalculatesAmounts()
    {
        var booking = NewDraft();
        var charge = booking.AddAdditionalCharge(ChargeCategory.Accessorial, "Extra", 1m, 10m, 15m, "Reason");

        booking.UpdateAdditionalCharge(charge.Id, "Extra v2", 3m, 20m, 30m, "Corrected", allowInvoicedDraft: false);

        Assert.Equal("Extra v2", charge.Description);
        Assert.Equal(60m, charge.BuyingAmount);
        Assert.Equal(90m, charge.SellingAmount);
    }

    [Fact]
    public void UpdateAdditionalCharge_UnknownId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            NewDraft().UpdateAdditionalCharge(Guid.NewGuid(), "X", 1m, 1m, 1m, "Reason", allowInvoicedDraft: false));
    }

    [Fact]
    public void GetTotals_SumsChargesAndComputesMargin()
    {
        var booking = NewDraft();
        var baseline = booking.GetTotals();

        booking.AddAdditionalCharge(ChargeCategory.Accessorial, "Extra", 1m, 100m, 150m, "Reason");
        var totals = booking.GetTotals();

        Assert.Equal(baseline.Buying + 100m, totals.Buying);
        Assert.Equal(baseline.Selling + 150m, totals.Selling);
        Assert.Equal(totals.Selling - totals.Buying, totals.Margin);
    }

    [Fact]
    public void BookingTotals_ZeroSelling_HasZeroMarginPercent()
    {
        Assert.Equal(0m, new BookingTotals("PHP", 100m, 0m).MarginPercent);
    }

    [Fact]
    public void MarkChargesInvoiced_ThenRelease_ReturnsChargesToUnbilled()
    {
        var booking = NewDraft();
        var charge = booking.AddAdditionalCharge(ChargeCategory.Accessorial, "Extra", 1m, 10m, 15m, "Reason");
        var invoiceId = Guid.NewGuid();

        booking.MarkChargesInvoiced(invoiceId, [charge.Id]);
        Assert.Equal(BillingStatus.Invoiced, charge.BillingStatus);
        Assert.DoesNotContain(charge, booking.GetUnbilledCharges());

        booking.ReleaseChargesFromInvoice(invoiceId);
        Assert.Equal(BillingStatus.Unbilled, charge.BillingStatus);
        Assert.Contains(charge, booking.GetUnbilledCharges());
    }

    [Fact]
    public void MarkChargesInvoiced_AlreadyInvoiced_Throws()
    {
        var booking = NewDraft();
        var charge = booking.AddAdditionalCharge(ChargeCategory.Accessorial, "Extra", 1m, 10m, 15m, "Reason");
        booking.MarkChargesInvoiced(Guid.NewGuid(), [charge.Id]);

        Assert.Throws<InvalidOperationException>(() => booking.MarkChargesInvoiced(Guid.NewGuid(), [charge.Id]));
    }

    #endregion

    #region Documents

    [Fact]
    public void VerifyDocument_UnknownId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => NewDraft().VerifyDocument(Guid.NewGuid(), "ops"));
    }

    [Fact]
    public void AddDocument_OnCancelledBooking_Throws()
    {
        var booking = NewDraft();
        booking.Cancel("ops", "Duplicate");

        Assert.Throws<InvalidOperationException>(() => booking.AddDocument(BookingDocumentType.Kyc));
    }

    [Fact]
    public void WaiveDocument_BlankReason_Throws()
    {
        var booking = NewDraft();
        var doc = booking.AddDocument(BookingDocumentType.Kyc);

        Assert.ThrowsAny<ArgumentException>(() => booking.WaiveDocument(doc.Id, "manager", " "));
    }

    #endregion

    #region Reschedule

    [Fact]
    public void Reschedule_DraftBooking_Throws()
    {
        var booking = NewDraft();

        Assert.Throws<InvalidOperationException>(() =>
            booking.Reschedule(SameRoute(booking, 5), "Customer asked", "ops"));
    }

    [Fact]
    public void Reschedule_ChangedWindows_UpdatesLaneAndRaisesEvent()
    {
        var booking = NewDraft();
        booking.Confirm("ops");
        var oldPickup = booking.RequestedPickupStart;

        booking.Reschedule(SameRoute(booking, 5), "Customer asked", "ops");

        Assert.NotEqual(oldPickup, booking.RequestedPickupStart);
        var evt = Assert.Single(booking.DomainEvents.OfType<BookingRescheduled>());
        Assert.Equal(oldPickup, evt.OldPickupStart);
        Assert.Equal(booking.RequestedPickupStart, evt.NewPickupStart);
        Assert.Equal("Customer asked", evt.Reason);
    }

    [Fact]
    public void Reschedule_UnchangedWindows_RaisesNoEvent()
    {
        var booking = NewDraft();
        booking.Confirm("ops");

        booking.Reschedule(SameRoute(booking, 0), "No-op", "ops");

        Assert.Empty(booking.DomainEvents.OfType<BookingRescheduled>());
    }

    [Fact]
    public void Reschedule_DifferentRoute_Throws()
    {
        var booking = NewDraft();
        booking.Confirm("ops");
        var differentRoute = LaneTransit.PointToPoint(
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            pickupStart: DateTimeOffset.UtcNow.AddDays(3),
            deliveryEnd: DateTimeOffset.UtcNow.AddDays(4),
            distanceKm: 100m);

        Assert.Throws<InvalidOperationException>(() => booking.Reschedule(differentRoute, "Reroute", "ops"));
    }

    [Fact]
    public void Reschedule_BlankReason_Throws()
    {
        var booking = NewDraft();
        booking.Confirm("ops");

        Assert.ThrowsAny<ArgumentException>(() => booking.Reschedule(SameRoute(booking, 5), " ", "ops"));
    }

    #endregion

    #region Helpers

    private static Booking NewDraft(bool creditCheckRequired = false) =>
        Booking.CreateDraftFromQuotation(AcceptedQuotation(), 1, customerCreditCheckRequired: creditCheckRequired);

    private static Quotation NewQuotation()
    {
        var lane = LaneTransit.PointToPoint(
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            pickupStart: DateTimeOffset.UtcNow,
            deliveryEnd: DateTimeOffset.UtcNow.AddDays(1),
            distanceKm: 100m);

        var cargo = CargoSpecification.Create(
            commodity: "General Cargo",
            totalWeightKg: 1000m,
            palletCount: 2,
            declaredCargoValue: new Money { Amount = 5000m, Currency = "USD" },
            requiresActiveRefrigeration: false);

        return Quotation.Create(
            customerId: Guid.NewGuid(),
            branchId: Guid.NewGuid(),
            referenceNumber: QuotationReference,
            currency: "USD",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: lane,
            cargoSpec: cargo);
    }

    private static Quotation DraftQuotation() => NewQuotation();

    private static Quotation AcceptedQuotation() => TestQuotations.Accepted(NewQuotation());

    /// <summary>Same stops and order as the booking's lane, with both windows shifted by <paramref name="shiftDays"/>.</summary>
    private static LaneTransit SameRoute(Booking booking, int shiftDays)
    {
        var stops = booking.Lane.Stops.OrderBy(s => s.Sequence).ToList();
        var shifted = stops.Select(s => RouteStop.Create(
            s.Sequence,
            s.LocationId,
            s.Type,
            s.WindowStart?.AddDays(shiftDays),
            s.WindowEnd?.AddDays(shiftDays),
            s.Instructions));

        return new LaneTransit(shifted, booking.Lane.TotalDistanceKm);
    }

    #endregion
}
