using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class QuotationTemperatureRequirementTests
{
    [Fact]
    public void Create_RefrigeratedCargoWithoutTemperatureSpec_Throws()
    {
        var cargo = CargoSpecification.Create(
            "Frozen food",
            100m,
            1,
            new Money { Amount = 0m, Currency = "PHP" },
            requiresActiveRefrigeration: true);

        Assert.Throws<InvalidOperationException>(() => CreateQuotation(cargo, null));
    }

    [Fact]
    public void Create_RefrigeratedCargoWithTemperatureSpec_Succeeds()
    {
        var cargo = CargoSpecification.Create(
            "Frozen food",
            100m,
            1,
            new Money { Amount = 0m, Currency = "PHP" },
            requiresActiveRefrigeration: true);
        var temperature = TemperatureControl.Create(
            TemperatureZone.Frozen,
            -20m,
            2m,
            ComplianceRegime.Standard,
            requiresTelematics: false);

        var quotation = CreateQuotation(cargo, temperature);

        Assert.Same(temperature, quotation.TemperatureSpec);
    }

    private static Quotation CreateQuotation(
        CargoSpecification cargo,
        TemperatureControl? temperature)
    {
        var lane = LaneTransit.PointToPoint(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            100m);

        return Quotation.Create(
            Guid.NewGuid(),
            branchId: Guid.NewGuid(),
            "QT-TEMP-001",
            "PHP",
            TransportMode.Road,
            FreightMode.Domestic,
            ShipmentType.FTL,
            Incoterm.DAP,
            lane,
            cargo,
            temperature);
    }
}
