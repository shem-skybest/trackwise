using MockQueryable;
using NSubstitute;
using Trackwise.Application.Modules.Financial.FuelLog.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FuelLog;

/// <summary>The slips Monday's Fleet Gas board let through are refused when the fill-up is typed.</summary>
public class RecordFuelFillUpHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Expense, Guid> expenseRepo = Substitute.For<ITenantRepository<Expense, Guid>>();
    private readonly ITenantRepository<Truck, Guid> truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
    private readonly ITenantRepository<Trip, Guid> tripRepo = Substitute.For<ITenantRepository<Trip, Guid>>();
    private readonly List<Expense> expenses = [];
    private readonly Truck truck = new() { Number = "ABD6777", Name = "ABD6777 (2T)", Type = TruckType.FreightTruck };
    private readonly RecordFuelFillUpHandler sut;

    private static readonly DateTime Sept28 = new(2026, 9, 28, 2, 0, 0, DateTimeKind.Utc);

    public RecordFuelFillUpHandlerTests()
    {
        tenantUow.GetCurrentTenant().Returns(new Tenant
        {
            Name = "ph",
            ConnectionString = "test",
            BillingEmail = "billing@test.com",
            CompanyAddress = new Address { StreetAddress = "1 Test St", CityOrMunicipality = "Pasay", Province = "Metro Manila", Region = "NCR", Zipcode = "1300", Country = "PH" },
            Settings = new TenantSettings { Currency = CurrencyCode.PHP, Timezone = "Asia/Manila" }
        });
        tenantUow.Repository<Expense>().Returns(expenseRepo);
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<Trip>().Returns(tripRepo);

        expenseRepo.Query().Returns(_ => expenses.BuildMock());
        expenseRepo.When(r => r.AddAsync(Arg.Any<Expense>(), Arg.Any<CancellationToken>()))
            .Do(c => expenses.Add(c.Arg<Expense>()));
        truckRepo.GetByIdAsync(truck.Id, Arg.Any<CancellationToken>()).Returns(truck);
        truckRepo.Query().Returns(_ => new List<Truck> { truck }.BuildMock());
        tripRepo.Query().Returns(_ => new List<Trip>().BuildMock());

        sut = new RecordFuelFillUpHandler(tenantUow);
    }

    private RecordFuelFillUpCommand Command(decimal liters = 35.55m, decimal amount = 3732.75m, int? odometer = null) => new()
    {
        TruckId = truck.Id,
        FilledAt = Sept28,
        Liters = liters,
        Amount = amount,
        FleetCard = FleetCard.Shell,
        OdometerReading = odometer
    };

    private void Logged(DateTime filledAt, int odometer) =>
        expenses.Add(TruckExpense.ForFuel(new FuelFillUp(truck.Id, filledAt, 40,
            new Money { Amount = 3600, Currency = "PHP" }, FleetCard.Shell, odometer, null, null, null)));

    [Fact]
    public async Task Handle_Valid_SavesForReviewInPesos()
    {
        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("PHP", result.Value!.Currency);
        Assert.Equal(105m, result.Value.PricePerLiter);
        Assert.Equal(ExpenseStatus.Pending, result.Value.Status);
        Assert.Equal("ABD6777", result.Value.TruckNumber);
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LitersAndAmountSwapped_Refused()
    {
        var result = await sut.Handle(Command(liters: 21372.42m, amount: 90.5m), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("a liter", result.Error);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameReceiptTwice_Refused()
    {
        await sut.Handle(Command(), CancellationToken.None);

        var again = Command();
        again.FilledAt = Sept28.AddHours(3);
        var result = await sut.Handle(again, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already logged", result.Error);
    }

    [Fact]
    public async Task Handle_OdometerBelowAnEarlierReading_Refused()
    {
        Logged(Sept28.AddDays(-2), 150_000);

        var result = await sut.Handle(Command(odometer: 149_500), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("too low", result.Error);
    }

    [Fact]
    public async Task Handle_OdometerAboveALaterReading_Refused()
    {
        Logged(Sept28.AddDays(2), 150_000);

        var result = await sut.Handle(Command(odometer: 151_000), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("too high", result.Error);
    }

    [Fact]
    public async Task Handle_OdometerAfterTheLastReading_WorksOutKmPerLiter()
    {
        Logged(Sept28.AddDays(-1), 150_000);

        var result = await sut.Handle(Command(liters: 35.55m, odometer: 150_320), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(320, result.Value!.KmSincePrevious);
        Assert.Equal(9.00m, result.Value.KmPerLiter);
    }

    [Fact]
    public async Task Handle_TripOnAnotherTruck_Refused()
    {
        var trip = new Trip { Name = "HFI schools", TruckId = Guid.NewGuid() };
        tripRepo.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);
        var command = Command();
        command.TripId = trip.Id;

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("different truck", result.Error);
    }

    [Fact]
    public async Task Handle_InTheFuture_Refused()
    {
        var command = Command();
        command.FilledAt = DateTime.UtcNow.AddDays(1);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
