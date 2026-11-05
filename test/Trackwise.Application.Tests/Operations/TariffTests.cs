using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class TariffTests
{
    // Stand-ins for master container-type rows. The entity holds a foreign key now, so these only
    // have to be stable and distinct - the tests are about the tariff, not the box.
    private static readonly Guid HighCube40 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Reefer40 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Create_SetsDetailsAndInitialVersion()
    {
        var tariff = Tariff.Create(
            mode: FreightMode.Import,
            serviceLevel: ServiceLevel.Standard,
            name: "Asia-US Tariff",
            currency: "usd",
            originLane: "CNSHA",
            destinationLane: "USLAX",
            containerTypeId: HighCube40,
            carrierId: null,
            effectiveFrom: new DateOnly(2026, 1, 1),
            contractId: null);

        Assert.Equal("Asia-US Tariff", tariff.Name);
        Assert.Equal(FreightMode.Import, tariff.Mode);
        Assert.Equal(ServiceLevel.Standard, tariff.ServiceLevel);
        Assert.Equal("USD", tariff.Currency);
        Assert.Equal("CNSHA", tariff.OriginLane);
        Assert.Equal("USLAX", tariff.DestinationLane);
        Assert.Equal(HighCube40, tariff.ContainerTypeId);
        Assert.Equal(1, tariff.Version);
        Assert.True(tariff.IsActive);
        Assert.Contains(tariff.DomainEvents, item => item is TariffCreated);
    }

    [Fact]
    public void UpdateDetails_ChangesDetailsAndIncrementsVersion()
    {
        var tariff = CreateTariff();

        tariff.UpdateDetails(
            mode: FreightMode.Export,
            serviceLevel: ServiceLevel.Express,
            name: "Updated Tariff",
            currency: "eur",
            effectiveFrom: new DateOnly(2026, 2, 1),
            effectiveTo: new DateOnly(2026, 12, 31),
            isActive: false,
            originLane: "USLAX",
            destinationLane: "CNSHA",
            containerTypeId: Reefer40,
            carrierId: null,
            contractId: null);

        Assert.Equal("Updated Tariff", tariff.Name);
        Assert.Equal(FreightMode.Export, tariff.Mode);
        Assert.Equal(ServiceLevel.Express, tariff.ServiceLevel);
        Assert.Equal("EUR", tariff.Currency);
        Assert.Equal(new DateOnly(2026, 2, 1), tariff.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), tariff.EffectiveTo);
        Assert.Equal("USLAX", tariff.OriginLane);
        Assert.Equal("CNSHA", tariff.DestinationLane);
        Assert.Equal(Reefer40, tariff.ContainerTypeId);
        Assert.Equal(2, tariff.Version);
        Assert.False(tariff.IsActive);
        Assert.Contains(tariff.DomainEvents, item => item is TariffUpdated);
    }

    [Fact]
    public void AddRate_CreatesRateAndRaisesEvent()
    {
        var tariff = CreateTariff();

        var rate = tariff.AddRate(
            chargeCodeId: Guid.NewGuid(),
            pricingModel: PricingModel.PerUnit,
            baseAmount: 150m,
            unit: "per day",
            minimum: 10m,
            maximum: 500m,
            freeDays: 5,
            rateAfterFreeDays: 150m,
            percentage: null,
            percentageBase: null,
            breakpointJson: "{\"tiers\":[]}");

        Assert.Equal(tariff.Id, rate.TariffId);
        Assert.Equal(150m, rate.BaseAmount);
        Assert.Equal("per day", rate.Unit);
        Assert.Equal(5, rate.FreeDays);
        Assert.Equal(150m, rate.RateAfterFreeDays);
        Assert.Contains(rate, tariff.Rates);
        Assert.Contains(tariff.DomainEvents, item => item is TariffRateAdded);
    }

    [Fact]
    public void HistoryCreate_StoresVersionTransitionAndRaisesEvent()
    {
        var tariffId = Guid.NewGuid();

        var history = TariffHistory.Create(
            tariffId: tariffId,
            oldVersion: 1,
            newVersion: 2,
            changedBy: "user-1",
            changeReason: "Updated lane");

        Assert.Equal(tariffId, history.TariffId);
        Assert.Equal(1, history.OldVersion);
        Assert.Equal(2, history.NewVersion);
        Assert.Contains(history.DomainEvents, item => item is TariffHistoryCreated);
    }

    private static Tariff CreateTariff()
    {
        return Tariff.Create(
            mode: FreightMode.Domestic,
            serviceLevel: ServiceLevel.Economy,
            name: "Test Tariff",
            currency: "USD",
            originLane: "A",
            destinationLane: "B",
            containerTypeId: null,
            carrierId: null,
            effectiveFrom: new DateOnly(2026, 1, 1),
            contractId: null);
    }
}
