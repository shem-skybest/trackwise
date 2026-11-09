using MockQueryable;
using NSubstitute;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotationRevision;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class CreateQuotationRevisionHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Branch, Guid> branchRepo = Substitute.For<ITenantRepository<Branch, Guid>>();
    private readonly Branch branch = new() { Code = "MNL", Name = "Manila" };
    private readonly ITenantRepository<Quotation, Guid> quotationRepo =
        Substitute.For<ITenantRepository<Quotation, Guid>>();
    private readonly IQuotationReferenceGenerator referenceGenerator =
        Substitute.For<IQuotationReferenceGenerator>();

    public CreateQuotationRevisionHandlerTests()
    {
        tenantUow.Repository<Quotation>().Returns(quotationRepo);
        tenantUow.Repository<Branch>().Returns(branchRepo);
        branchRepo.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);
        branchRepo.Query().Returns(_ => new List<Branch> { branch }.BuildMock());
    }

    [Fact]
    public async Task Handle_CreatesDraftRevisionWithCopiedLinesAndImmutableSourcePrices()
    {
        var source = CreateQuotation();
        var tariff = Tariff.Create(
            FreightMode.Domestic,
            ServiceLevel.Standard,
            "Domestic rates",
            "USD",
            null,
            null,
            null,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null);
        var rate = tariff.AddRate(
            Guid.NewGuid(),
            PricingModel.Flat,
            100m,
            "shipment",
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        rate.Tariff = tariff;
        var sourceLine = source.AddLine(
            rate,
            ChargeCategory.Base,
            OperationalServiceType.Standard,
            1m,
            MarkupType.Percentage,
            10m,
            null,
            "Original charge");
        source.Approve("manager@test.com");
        source.Issue(TimeSpan.FromDays(14));

        quotationRepo.Query().Returns(new List<Quotation> { source }.BuildMock());
        Quotation? createdRevision = null;
        quotationRepo.AddAsync(
                Arg.Do<Quotation>(quotation => createdRevision = quotation),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // FIXED: Return the exact reference number the assertion expects
        referenceGenerator.NextAsync(Arg.Any<FreightMode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("QT-2026-DOM-00002");

        var sut = new CreateQuotationRevisionHandler(tenantUow, referenceGenerator);
        var result = await sut.Handle(new CreateQuotationRevisionCommand(source.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var revision = Assert.IsType<Quotation>(createdRevision);
        Assert.Equal(result.Value, revision.Id);
        Assert.Equal("QT-2026-DOM-00002", revision.ReferenceNumber);
        Assert.Equal(QuotationStatus.Draft, revision.Status);
        Assert.Equal(source.RevisionGroupId, revision.RevisionGroupId);
        Assert.Equal(2, revision.RevisionNumber);
        Assert.Equal(source.Id, revision.PreviousRevisionId);
        Assert.Equal(source.CustomerId, revision.CustomerId);
        Assert.Equal(source.FreightMode, revision.FreightMode);
        Assert.Equal(
            source.Lane.Stops.Select(stop => stop.LocationId),
            revision.Lane.Stops.Select(stop => stop.LocationId));
        Assert.Single(revision.Lines);
        Assert.NotEqual(sourceLine.Id, revision.Lines.Single().Id);
        Assert.Equal(sourceLine.TariffRateId, revision.Lines.Single().TariffRateId);
        Assert.Equal(sourceLine.MarkupValue, revision.Lines.Single().MarkupValue);
        Assert.Null(revision.Lines.Single().SnapshotSellingAmount);
        Assert.NotNull(sourceLine.SnapshotSellingAmount);
        Assert.NotEqual(sourceLine.Id, revision.Id);
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DraftQuotation_Fails()
    {
        var source = CreateQuotation();
        quotationRepo.Query().Returns(new List<Quotation> { source }.BuildMock());

        var sut = new CreateQuotationRevisionHandler(tenantUow, referenceGenerator);
        var result = await sut.Handle(new CreateQuotationRevisionCommand(source.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("draft", result.Error, StringComparison.OrdinalIgnoreCase);
        await quotationRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_QuotationWithExistingRevision_Fails()
    {
        var source = CreateQuotation();
        SetStatus(source, QuotationStatus.Approved);
        var existingRevision = source.CreateRevision("QT-2026-DOM-00002", 2);
        quotationRepo.Query().Returns(new List<Quotation> { source, existingRevision }.BuildMock());

        var sut = new CreateQuotationRevisionHandler(tenantUow, referenceGenerator);
        var result = await sut.Handle(new CreateQuotationRevisionCommand(source.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("latest", result.Error, StringComparison.OrdinalIgnoreCase);
        await quotationRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // FIXED: Removed 'static' and tied the BranchId to the mocked class field
    private Quotation CreateQuotation()
    {
        var lane = LaneTransit.PointToPoint(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            100m);

        var cargo = CargoSpecification.Create(
            "General Cargo",
            100m,
            1,
            new Money { Amount = 1000m, Currency = "USD" });

        var quotation = Quotation.Create(
            Guid.NewGuid(),
            branch.Id, // Ties to your mocked branch
            "QT-2026-DOM-00001",
            "USD",
            TransportMode.Road,
            FreightMode.Domestic,
            ShipmentType.FTL,
            Incoterm.DAP,
            lane,
            cargo);

        // FIX: Manually set the navigation property so source.Branch.Code works!
        typeof(Quotation).GetProperty(nameof(Quotation.Branch))!.SetValue(quotation, branch);

        return quotation;
    }

    private static void SetStatus(Quotation quotation, QuotationStatus status) =>
        typeof(Quotation).GetProperty(nameof(Quotation.Status))!.SetValue(quotation, status);
}
