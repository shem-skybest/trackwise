using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class ChargeCodeTests
{
    [Fact]
    public void Create_NormalizesCodeAndSetsDefaults()
    {
        var chargeCode = ChargeCode.Create(
            code: "ofr",
            name: "Ocean Freight",
            category: ChargeCategory.Base,
            modes: [FreightMode.Import, FreightMode.Export],
            defaultUnit: "per shipment",
            requiresApproval: true,
            description: "Base ocean freight",
            taxCode: "VAT");

        Assert.Equal("OFR", chargeCode.Code);
        Assert.Equal("Ocean Freight", chargeCode.Name);
        Assert.Equal(ChargeCategory.Base, chargeCode.Category);
        Assert.Equal([FreightMode.Import, FreightMode.Export], chargeCode.ApplicableModes);
        Assert.Equal("per shipment", chargeCode.DefaultUnit);
        Assert.True(chargeCode.RequiresApproval);
        Assert.Equal("Base ocean freight", chargeCode.Description);
        Assert.Equal("VAT", chargeCode.TaxCode);
        Assert.True(chargeCode.IsActive);
        Assert.Contains(chargeCode.DomainEvents, item => item is ChargeCodeCreated);
    }

    [Fact]
    public void UpdateDetails_ChangesProvidedValuesAndRaisesEvent()
    {
        var chargeCode = ChargeCode.Create(
            code: "DEM",
            name: "Demurrage",
            category: ChargeCategory.Accessorial,
            modes: [FreightMode.Import],
            defaultUnit: "per day");

        chargeCode.UpdateDetails(
            name: "Updated Demurrage",
            description: "Storage after free time",
            category: ChargeCategory.Surcharge,
            modes: [FreightMode.Import, FreightMode.Export],
            defaultUnit: "per container day",
            requiresApproval: true,
            taxCode: "VAT");

        Assert.Equal("Updated Demurrage", chargeCode.Name);
        Assert.Equal("Storage after free time", chargeCode.Description);
        Assert.Equal(ChargeCategory.Surcharge, chargeCode.Category);
        Assert.Equal([FreightMode.Import, FreightMode.Export], chargeCode.ApplicableModes);
        Assert.Equal("per container day", chargeCode.DefaultUnit);
        Assert.True(chargeCode.RequiresApproval);
        Assert.Equal("VAT", chargeCode.TaxCode);
        Assert.Contains(chargeCode.DomainEvents, item => item is ChargeCodeUpdated);
    }

    [Fact]
    public void Deactivate_MarksCodeInactiveAndRaisesEvent()
    {
        var chargeCode = ChargeCode.Create(
            code: "BAF",
            name: "Bunker Adjustment Factor",
            category: ChargeCategory.Surcharge,
            modes: [FreightMode.Export],
            defaultUnit: "per shipment");

        chargeCode.Deactivate();

        Assert.False(chargeCode.IsActive);
        Assert.Contains(chargeCode.DomainEvents, item => item is ChargeCodeDeactivated);
    }
}
