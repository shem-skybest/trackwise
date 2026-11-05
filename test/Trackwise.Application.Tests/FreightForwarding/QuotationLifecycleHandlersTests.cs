using NSubstitute;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AcceptQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AddQuotationLine;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ApproveQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ExpireQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.IssueQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RejectQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.SubmitQuotation;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class QuotationLifecycleHandlersTests
{
    private readonly ITenantUnitOfWork _tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Quotation, Guid> _quotationRepo = Substitute.For<ITenantRepository<Quotation, Guid>>();
    private readonly ITenantRepository<QuotationLine, Guid> _quotationLineRepo = Substitute.For<ITenantRepository<QuotationLine, Guid>>();
    private readonly ITenantRepository<TariffRate, Guid> _tariffRateRepo = Substitute.For<ITenantRepository<TariffRate, Guid>>();

    public QuotationLifecycleHandlersTests()
    {
        _tenantUow.Repository<Quotation>().Returns(_quotationRepo);
        _tenantUow.Repository<QuotationLine>().Returns(_quotationLineRepo);
        _tenantUow.Repository<TariffRate>().Returns(_tariffRateRepo);
    }

    #region AddQuotationLine

    [Fact]
    public async Task AddQuotationLine_NonDraftQuotation_Fails()
    {
        var quotation = CreateTestQuotation();
        quotation.Approve("manager@test.com"); // Moves to Approved
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new AddQuotationLineHandler(_tenantUow);
        var command = new AddQuotationLineCommand
        {
            QuotationId = quotation.Id,
            TariffRateId = Guid.NewGuid(),
            Quantity = 10,
            MarkupType = MarkupType.Percentage,
            MarkupValue = 15m
        };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Draft", result.Error);
    }

    #endregion

    #region Submit

    [Fact]
    public async Task Submit_StandardCargoWithLines_AutoApproves()
    {
        var quotation = CreateTestQuotation(declaredValue: 50000m);
        AddPricedLine(quotation);
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new SubmitQuotationHandler(_tenantUow);
        var result = await sut.Handle(new SubmitQuotationCommand { Id = quotation.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(QuotationStatus.Approved, quotation.Status);
        Assert.Equal("SYSTEM_AUTO_APPROVER", quotation.ApprovedBy);
        await _tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_HighValueCargo_EscalatesToPendingApproval()
    {
        // Declared value > $500,000 triggers policy escalation
        var quotation = CreateTestQuotation(declaredValue: 750000m);
        AddPricedLine(quotation);
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new SubmitQuotationHandler(_tenantUow);
        var result = await sut.Handle(new SubmitQuotationCommand { Id = quotation.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(QuotationStatus.PendingApproval, quotation.Status);
        Assert.True(quotation.RequiresSpecialApproval);
        Assert.NotNull(quotation.ApprovalEscalationReason);
    }

    [Fact]
    public async Task Submit_QuotationWithoutLines_Fails()
    {
        var quotation = CreateTestQuotation(); // 0 lines
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new SubmitQuotationHandler(_tenantUow);
        var result = await sut.Handle(new SubmitQuotationCommand { Id = quotation.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("without priced line items", result.Error);
    }

    #endregion

    #region Approve

    [Fact]
    public async Task Approve_PendingApprovalQuotation_SetsApprovedByAndTimestamp()
    {
        var quotation = CreateTestQuotation(declaredValue: 600000m);
        AddPricedLine(quotation);
        quotation.Submit(); // Moves to PendingApproval
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new ApproveQuotationHandler(_tenantUow);
        var result = await sut.Handle(
            new ApproveQuotationCommand { Id = quotation.Id, ApprovedBy = "director@test.com" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(QuotationStatus.Approved, quotation.Status);
        Assert.Equal("director@test.com", quotation.ApprovedBy);
        Assert.NotNull(quotation.ApprovedAt);
    }

    #endregion

    #region Issue

    [Fact]
    public async Task Issue_ApprovedQuotation_SnapshotsPricesAndSetsExpiration()
    {
        var quotation = CreateTestQuotation();
        AddPricedLine(quotation);
        quotation.Approve("manager@test.com");

        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);
        _quotationRepo.Query().Returns(new[] { quotation }.AsQueryable());

        var sut = new IssueQuotationHandler(_tenantUow);
        var result = await sut.Handle(
            new IssueQuotationCommand { Id = quotation.Id, ValidityDays = 10 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(QuotationStatus.Issued, quotation.Status);
        Assert.NotNull(quotation.ValidUntil);
        Assert.True(quotation.ValidUntil.Value > DateTimeOffset.UtcNow.AddDays(9));

        // Verifies financial snapshotting took place
        var line = quotation.Lines.First();
        Assert.NotNull(line.SnapshotBuyingAmount);
        Assert.NotNull(line.SnapshotSellingAmount);
    }

    [Fact]
    public async Task Issue_DraftQuotation_Fails()
    {
        var quotation = CreateTestQuotation();
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);
        _quotationRepo.Query().Returns(new[] { quotation }.AsQueryable());

        var sut = new IssueQuotationHandler(_tenantUow);
        var result = await sut.Handle(new IssueQuotationCommand { Id = quotation.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Only Approved quotations can be issued", result.Error);
    }

    #endregion

    #region Accept

    [Fact]
    public async Task Accept_IssuedActiveQuotation_TransitionsToAccepted()
    {
        var quotation = CreateTestQuotation();
        AddPricedLine(quotation);
        quotation.Approve("manager@test.com");
        quotation.Issue(TimeSpan.FromDays(7)); // Valid for 7 days
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new AcceptQuotationHandler(_tenantUow);
        var result = await sut.Handle(new AcceptQuotationCommand { Id = quotation.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(QuotationStatus.Accepted, quotation.Status);
        Assert.NotNull(quotation.AcceptedAt);
    }

    [Fact]
    public async Task Accept_UnissuedQuotation_Fails()
    {
        var quotation = CreateTestQuotation();
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new AcceptQuotationHandler(_tenantUow);
        var result = await sut.Handle(new AcceptQuotationCommand { Id = quotation.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Only Issued quotations can be accepted", result.Error);
    }

    #endregion

    #region Expire

    [Fact]
    public async Task Expire_PastExpirationDate_TransitionsToExpired()
    {
        var quotation = CreateTestQuotation();
        AddPricedLine(quotation);
        quotation.Approve("manager@test.com");
        quotation.Issue(TimeSpan.FromDays(-1)); // Already in the past
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new ExpireQuotationHandler(_tenantUow);
        var result = await sut.Handle(new ExpireQuotationCommand { Id = quotation.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(QuotationStatus.Expired, quotation.Status);
    }

    [Fact]
    public async Task Expire_BeforeExpirationDate_Fails()
    {
        var quotation = CreateTestQuotation();
        AddPricedLine(quotation);
        quotation.Approve("manager@test.com");
        quotation.Issue(TimeSpan.FromDays(14)); // Future expiration
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new ExpireQuotationHandler(_tenantUow);
        var result = await sut.Handle(new ExpireQuotationCommand { Id = quotation.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("has not yet reached its expiration date", result.Error);
    }

    #endregion

    #region Reject

    [Fact]
    public async Task Reject_IssuedQuotation_TransitionsToRejectedWithReason()
    {
        var quotation = CreateTestQuotation();
        AddPricedLine(quotation);
        quotation.Approve("manager@test.com");
        quotation.Issue(TimeSpan.FromDays(7));
        _quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);

        var sut = new RejectQuotationHandler(_tenantUow);
        var result = await sut.Handle(
            new RejectQuotationCommand { Id = quotation.Id, RejectedBy = "shipper@client.com", Reason = "Competitor offered lower rate." },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(QuotationStatus.Rejected, quotation.Status);
        Assert.Equal("shipper@client.com", quotation.RejectedBy);
        Assert.Equal("Competitor offered lower rate.", quotation.RejectionReason);
    }

    #endregion

    #region Test Helpers

    private static Quotation CreateTestQuotation(decimal declaredValue = 25000m)
    {
        var lane = LaneTransit.PointToPoint(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(2),
            120m);

        var cargo = CargoSpecification.Create(
            commodity: "Standard Palletized Freight",
            totalWeightKg: 4000m,
            palletCount: 8,
            declaredCargoValue: new Money { Amount = declaredValue, Currency = "USD" },
            requiresActiveRefrigeration: false);

        return Quotation.Create(
            customerId: Guid.NewGuid(),
            referenceNumber: "QT-TEST-2026",
            currency: "USD",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: lane,
            cargoSpec: cargo);
    }

    private static void AddPricedLine(Quotation quotation)
    {
        var chargeCode = ChargeCode.Create("FRT", "Freight", ChargeCategory.Base, [FreightMode.Domestic], "km");
        chargeCode.Currency = "USD";

        var tariffRate = new TariffRate
        {
            ChargeCode = chargeCode,
            PricingModel = PricingModel.PerUnit,
            BaseAmount = 10m,
            Unit = "km"
        };

        var line = quotation.AddLine(
            tariffRateId: tariffRate.Id,
            category: ChargeCategory.Base,
            serviceType: OperationalServiceType.LineHaul,
            quantity: 100m,
            markupType: MarkupType.Percentage,
            markupValue: 10m,
            fxRate: null,
            notes: null);

        // Inject navigation property to support line.Price()
        typeof(QuotationLine).GetProperty(nameof(QuotationLine.TariffRate))?.SetValue(line, tariffRate);
    }

    #endregion
}
