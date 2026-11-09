using MockQueryable;
using NSubstitute;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Quotations;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class CreateQuotationHandlerTests
{
    private readonly ITenantUnitOfWork _tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Customer, Guid> _customerRepo = Substitute.For<ITenantRepository<Customer, Guid>>();
    private readonly ITenantRepository<Location, Guid> _locationRepo = Substitute.For<ITenantRepository<Location, Guid>>();
    private readonly ITenantRepository<Quotation, Guid> _quotationRepo = Substitute.For<ITenantRepository<Quotation, Guid>>();
    private readonly IQuotationRatingEngine _ratingEngine = Substitute.For<IQuotationRatingEngine>();
    private readonly IQuotationReferenceGenerator referenceGenerator = Substitute.For<IQuotationReferenceGenerator>();
    private readonly IFxRateService fxRateService = Substitute.For<IFxRateService>();

    private readonly Customer _customer;
    private readonly Location _origin;
    private readonly Location _destination;
    private readonly CreateQuotationHandler _sut;

    public CreateQuotationHandlerTests()
    {
        _customer = new Customer { Name = "BioPharma Corp" };

        _origin = new Location
        {
            Code = "BOS-PLANT-01",
            Name = "Boston Manufacturing Plant",
            Type = LocationType.Plant,
            IsActive = true,
            Latitude = 42.3601,
            Longitude = -71.0589
        };

        _destination = new Location
        {
            Code = "RDU-DC-02",
            Name = "Raleigh Distribution Center",
            Type = LocationType.DeliveryPoint,
            IsActive = true,
            Latitude = 35.7796,
            Longitude = -78.6382
        };

        _tenantUow.Repository<Customer>().Returns(_customerRepo);
        _tenantUow.Repository<Location>().Returns(_locationRepo);
        _tenantUow.Repository<Quotation>().Returns(_quotationRepo);

        _customerRepo.GetByIdAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(_customer);
        _locationRepo.GetByIdAsync(_origin.Id, Arg.Any<CancellationToken>()).Returns(_origin);
        _locationRepo.GetByIdAsync(_destination.Id, Arg.Any<CancellationToken>()).Returns(_destination);
        _locationRepo.Query().Returns(_ => new List<Location> { _origin, _destination }.BuildMock());
        fxRateService.GetRateAsync("USD", "PHP", Arg.Any<CancellationToken>()).Returns(56m);
        referenceGenerator.NextAsync(FreightMode.Domestic, "", Arg.Any<CancellationToken>()).Returns("QT-DOM-0001");

        _sut = new CreateQuotationHandler(_tenantUow, _ratingEngine, referenceGenerator, fxRateService);
    }

    [Fact]
    public async Task Handle_ValidPointToPointCommand_CreatesQuotationAndInvokesRatingEngine()
    {
        var command = BuildDefaultCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotEqual(Guid.Empty, result.Value);

        await _ratingEngine.Received(1)
            .AutoPriceAsync(Arg.Is<Quotation>(q => q.Id == result.Value), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());

        await _quotationRepo.Received(1).AddAsync(Arg.Is<Quotation>(q => q.Id == result.Value), Arg.Any<CancellationToken>());
        await _tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ReturnsFailure()
    {
        var unknownCustomerId = Guid.NewGuid();
        _customerRepo.GetByIdAsync(unknownCustomerId, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var command = BuildDefaultCommand();
        command.CustomerId = unknownCustomerId;

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
        await _quotationRepo.DidNotReceive().AddAsync(Arg.Any<Quotation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OriginLocationIsZone_ReturnsFailure()
    {
        _origin.Type = LocationType.Zone; // Zones cannot be driven to

        var command = BuildDefaultCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Zone grouping", result.Error);
    }

    [Fact]
    public async Task Handle_OriginBelongsToDifferentCustomer_ReturnsFailure()
    {
        _origin.CustomerId = Guid.NewGuid(); // Belongs to another client

        var command = BuildDefaultCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not belong to customer", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_OmittedDistanceWithValidGpsCoordinates_AutoCalculatesHaversineDistance()
    {
        var command = BuildDefaultCommand();
        command.Lane.TotalDistanceKm = null; // Handler must calculate distance

        Quotation? capturedQuotation = null;
        await _quotationRepo.AddAsync(Arg.Do<Quotation>(q => capturedQuotation = q), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(capturedQuotation);
        Assert.True(capturedQuotation.Lane.TotalDistanceKm > 800m); // Boston to Raleigh is ~850-900 km air
    }

    [Fact]
    public async Task Handle_OmittedDistanceWithoutGpsCoordinates_ReturnsFailure()
    {
        _origin.Latitude = null;
        _origin.Longitude = null;

        var command = BuildDefaultCommand();
        command.Lane.TotalDistanceKm = null;

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("surveyed GPS coordinates", result.Error);
    }

    private CreateQuotationCommand BuildDefaultCommand()
    {
        return new CreateQuotationCommand
        {
            CustomerId = _customer.Id,
            Currency = "USD",
            Mode = TransportMode.Road,
            FreightMode = FreightMode.Domestic,
            ShipmentType = ShipmentType.FTL,
            Incoterm = Incoterm.DAP,
            Lane = new LaneTransitDto
            {
                OriginLocationId = _origin.Id,
                DestinationLocationId = _destination.Id,
                RequestedPickupWindowStart = DateTimeOffset.UtcNow.AddDays(1),
                RequestedDeliveryWindowEnd = DateTimeOffset.UtcNow.AddDays(3),
                TotalDistanceKm = 880m
            },
            Cargo = new CargoSpecificationDto(
                Commodity: "Pharmaceutical Vaccines",
                TotalWeightKg: 2500m,
                PalletCount: 6,
                DeclaredCargoValueAmount: 150000m,
                DeclaredCargoValueCurrency: "USD",
                RequiresActiveRefrigeration: true),
            TemperatureSpec = new TemperatureControlDto(
                    TemperatureZone.Chilled,
                    4.0m,
                    1.5m,
                    ComplianceRegime.GDP,
                    true)
        };
    }
}
