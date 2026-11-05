using MockQueryable;
using NSubstitute;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class PromoteBookingToWorkfileHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Booking, Guid> bookingRepo =
        Substitute.For<ITenantRepository<Booking, Guid>>();
    private readonly ITenantRepository<Workfile, Guid> workfileRepo =
        Substitute.For<ITenantRepository<Workfile, Guid>>();
    private readonly ITenantRepository<BusinessPartner, Guid> partnerRepo =
        Substitute.For<ITenantRepository<BusinessPartner, Guid>>();
    private readonly IWorkfileReferenceGenerator referenceGenerator =
        Substitute.For<IWorkfileReferenceGenerator>();

    private readonly Booking booking;
    private readonly PromoteBookingToWorkfileHandler sut;

    public PromoteBookingToWorkfileHandlerTests()
    {
        var quotation = CreateTestQuotation();
        quotation.Approve("user-1");
        booking = Booking.CreateDraftFromQuotation(quotation);
        booking.Confirm("user-1");

        tenantUow.Repository<Booking>().Returns(bookingRepo);
        tenantUow.Repository<Workfile>().Returns(workfileRepo);
        tenantUow.Repository<BusinessPartner>().Returns(partnerRepo);

        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        workfileRepo.Query().Returns(new List<Workfile>().BuildMock());
        partnerRepo.Query().Returns(new List<BusinessPartner>().BuildMock());
        referenceGenerator.NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => $"{call.Arg<string>()}-000001");

        sut = new PromoteBookingToWorkfileHandler(tenantUow, referenceGenerator);
    }

    private PromoteBookingToWorkfileCommand RegularImport() => new()
    {
        BookingId = booking.Id,
        Type = WorkfileType.Regular,
        Direction = FreightMode.Import,
        BillIndicator = BillIndicator.Hbl,
        ReportCategory = ReportCategory.Psb
    };

    [Fact]
    public async Task Promoting_AllocatesAReferenceFromTheTypesSeries()
    {
        var result = await sut.Handle(RegularImport(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await referenceGenerator.Received(1).NextAsync("BKN-IMP", Arg.Any<CancellationToken>());
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PromotingAPendingBooking_Fails()
    {
        var quotation = CreateTestQuotation();
        quotation.Approve("user-1");
        var pending = Booking.CreateDraftFromQuotation(quotation);
        bookingRepo.GetByIdAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);

        var command = RegularImport();
        command.BookingId = pending.Id;

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Confirmed", result.Error);
        await workfileRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task PromotingABookingTwice_Fails()
    {
        var existing = RegularWorkfile.Create(
            "BKN-IMP-000001", FreightMode.Import, booking.CustomerId, booking.Id,
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });
        workfileRepo.Query().Returns(new List<Workfile> { existing }.BuildMock());

        var result = await sut.Handle(RegularImport(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already has a workfile", result.Error);
    }

    [Fact]
    public async Task ARegularFileWithoutABooking_Fails()
    {
        var command = RegularImport();
        command.BookingId = null;
        command.CustomerId = Guid.NewGuid();

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("promoted from a booking", result.Error);
    }

    [Fact]
    public async Task AReeferFileWithoutABooking_IsAllowed()
    {
        // The one type that is not booked against a carrier, and the reason BookingId is nullable.
        var result = await sut.Handle(
            new PromoteBookingToWorkfileCommand
            {
                Type = WorkfileType.Reefer,
                CustomerId = Guid.NewGuid(),
                ReportCategory = ReportCategory.NotPsb
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await referenceGenerator.Received(1).NextAsync("BKN-RFV", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AReeferFileWithNoBookingAndNoCustomer_Fails()
    {
        var result = await sut.Handle(
            new PromoteBookingToWorkfileCommand
            {
                Type = WorkfileType.Reefer,
                ReportCategory = ReportCategory.NotPsb
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("customer is required", result.Error);
    }

    [Fact]
    public async Task ARegularFileWithoutABillIndicator_Fails()
    {
        var command = RegularImport();
        command.BillIndicator = null;

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("MBL/HBL", result.Error);
    }

    [Fact]
    public async Task AReeferFileNeedsNoBillIndicator()
    {
        var result = await sut.Handle(
            new PromoteBookingToWorkfileCommand
            {
                Type = WorkfileType.Reefer,
                CustomerId = Guid.NewGuid(),
                ReportCategory = ReportCategory.Psb
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ADuplicateBlNumber_IsRejectedOnANonReeferFile()
    {
        var existing = RegularWorkfile.Create(
            "BKN-IMP-000001", FreightMode.Import, Guid.NewGuid(), Guid.NewGuid(),
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb, BillOfLadingNumber = "MAEU123" });
        workfileRepo.Query().Returns(new List<Workfile> { existing }.BuildMock());

        var command = RegularImport();
        command.BillOfLadingNumber = "MAEU123";

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task ADuplicateBlNumber_IsIgnoredOnAReeferFile()
    {
        // Reefer files share no BL, so the duplicate check would reject perfectly valid work.
        var existing = ReeferWorkfile.Create(
            "BKN-RFV-000001", Guid.NewGuid(), null,
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb, BillOfLadingNumber = "SAME" });
        workfileRepo.Query().Returns(new List<Workfile> { existing }.BuildMock());

        var result = await sut.Handle(
            new PromoteBookingToWorkfileCommand
            {
                Type = WorkfileType.Reefer,
                CustomerId = Guid.NewGuid(),
                ReportCategory = ReportCategory.Psb,
                BillOfLadingNumber = "SAME"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AnInactivePartner_IsRejected()
    {
        // Approved and then retired, which is the case this test is about. A partner that was never
        // approved is refused too, but for a different reason and with a different way out - see
        // WorkfilePartnerValidation.
        var retired = new BusinessPartner
        {
            Name = "Old Line",
            Code = "OLD",
            Roles = [BusinessPartnerRole.ShippingLine],
            ApprovalState = MasterDataApprovalState.Approved,
            IsActive = false
        };
        partnerRepo.Query().Returns(new List<BusinessPartner> { retired }.BuildMock());

        var command = RegularImport();
        command.ShippingLineId = retired.Id;

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive", result.Error);
    }

    [Fact]
    public async Task APartnerWithoutTheMatchingRole_IsRejected()
    {
        var trucker = new BusinessPartner
        {
            Name = "Just A Trucker",
            Code = "TRK",
            Roles = [BusinessPartnerRole.Trucker],
            ApprovalState = MasterDataApprovalState.Approved,
            IsActive = true
        };
        partnerRepo.Query().Returns(new List<BusinessPartner> { trucker }.BuildMock());

        var command = RegularImport();
        command.ShippingLineId = trucker.Id;

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not registered as a shipping line", result.Error);
    }

    [Fact]
    public async Task AReferenceOverride_SkipsAllocationButMustBeUnique()
    {
        var command = RegularImport();
        command.ReferenceNumber = "BKN-IMP-999999";

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await referenceGenerator.DidNotReceiveWithAnyArgs().NextAsync(default!, default);
    }

    [Fact]
    public async Task AReferenceOverrideThatIsTaken_Fails()
    {
        var existing = RegularWorkfile.Create(
            "BKN-IMP-999999", FreightMode.Import, Guid.NewGuid(), Guid.NewGuid(),
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });
        workfileRepo.Query().Returns(new List<Workfile> { existing }.BuildMock());

        var command = RegularImport();
        command.ReferenceNumber = "BKN-IMP-999999";

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already in use", result.Error);
    }

    [Fact]
    public async Task TheCustomerIsCarriedOverFromTheBooking()
    {
        Workfile? saved = null;
        await workfileRepo.AddAsync(Arg.Do<Workfile>(w => saved = w), Arg.Any<CancellationToken>());

        var result = await sut.Handle(RegularImport(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(booking.CustomerId, saved!.CustomerId);
        Assert.Equal(booking.Id, saved.BookingId);
    }

    private static Quotation CreateTestQuotation()
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

        var quotation = Quotation.Create(
            customerId: Guid.NewGuid(),
            referenceNumber: "QT-TEST-001",
            currency: "USD",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: lane,
            cargoSpec: cargo);

        quotation.Approve("manager@test.com");
        return quotation;
    }
}
