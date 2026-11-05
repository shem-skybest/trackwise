using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// The per-type rules are the whole point of making shipment type a TPH discriminator rather than a
/// flag. Reefer is the odd one out on three separate rules, and getting any of them wrong rejects
/// valid files (or accepts invalid ones) with no other symptom.
/// </summary>
public class WorkfileTypeRulesTests
{
    private static readonly WorkfileHeaderData Header = new() { ReportCategory = ReportCategory.Psb };

    [Theory]
    [InlineData(FreightMode.Domestic, "BKN-DOM")]
    [InlineData(FreightMode.Import, "BKN-IMP")]
    [InlineData(FreightMode.Export, "BKN-EXP")]
    [InlineData(FreightMode.Triangular, "BKN-TRI")]
    public void RegularFile_DrawsItsSeriesFromDirection(FreightMode direction, string expected)
    {
        var workfile = RegularWorkfile.Create("x", direction, Guid.NewGuid(), Guid.NewGuid(), Header);

        Assert.Equal(expected, workfile.ReferenceSeries);
        Assert.Equal(expected, WorkfileSeries.For(WorkfileType.Regular, direction, null));
    }

    [Theory]
    [InlineData(TruckingVariant.Truck, "BKN-TRK")]
    [InlineData(TruckingVariant.Trailer, "BKN-TRL")]
    public void TruckingFile_DrawsItsSeriesFromVariant(TruckingVariant variant, string expected)
    {
        var workfile = TruckingWorkfile.Create("x", variant, Guid.NewGuid(), Guid.NewGuid(), Header);

        Assert.Equal(expected, workfile.ReferenceSeries);
        Assert.Equal(expected, WorkfileSeries.For(WorkfileType.Trucking, null, variant));
    }

    [Fact]
    public void ReeferFile_UsesTheReeferSeries()
    {
        var workfile = ReeferWorkfile.Create("x", Guid.NewGuid(), null, Header);

        Assert.Equal("BKN-RFV", workfile.ReferenceSeries);
        Assert.Equal("BKN-RFV", WorkfileSeries.For(WorkfileType.Reefer, null, null));
    }

    [Fact]
    public void ReeferFile_SkipsBookingBillIndicatorAndDuplicateBlChecks()
    {
        var workfile = ReeferWorkfile.Create("x", Guid.NewGuid(), null, Header);

        Assert.False(workfile.RequiresBookingNumber);
        Assert.False(workfile.RequiresBillIndicator);
        Assert.False(workfile.RequiresDuplicateBlCheck);
        Assert.True(workfile.RequiresDeliveryLocation);
    }

    [Theory]
    [InlineData(WorkfileType.Regular)]
    [InlineData(WorkfileType.Trucking)]
    public void NonReeferFiles_EnforceAllThree(WorkfileType type)
    {
        var rules = WorkfileRules.For(type);

        Assert.True(rules.RequiresBookingNumber);
        Assert.True(rules.RequiresBillIndicator);
        Assert.True(rules.RequiresDuplicateBlCheck);
        Assert.False(rules.RequiresDeliveryLocation);
    }

    [Fact]
    public void OnlyExports_PromptForAVatTreatment()
    {
        var export = RegularWorkfile.Create("x", FreightMode.Export, Guid.NewGuid(), Guid.NewGuid(), Header);
        var import = RegularWorkfile.Create("x", FreightMode.Import, Guid.NewGuid(), Guid.NewGuid(), Header);
        var reefer = ReeferWorkfile.Create("x", Guid.NewGuid(), null, Header);
        var trucking = TruckingWorkfile.Create("x", TruckingVariant.Truck, Guid.NewGuid(), Guid.NewGuid(), Header);

        Assert.True(export.PromptsForVatTreatment);
        Assert.False(import.PromptsForVatTreatment);
        Assert.False(reefer.PromptsForVatTreatment);
        Assert.False(trucking.PromptsForVatTreatment);
    }

    [Fact]
    public void GarageTrackingAndBudget_AreTruckingSideOnly()
    {
        Assert.False(WorkfileRules.For(WorkfileType.Regular).SupportsGarageTracking);
        Assert.True(WorkfileRules.For(WorkfileType.Reefer).SupportsGarageTracking);
        Assert.True(WorkfileRules.For(WorkfileType.Trucking).SupportsGarageTracking);

        Assert.False(WorkfileRules.For(WorkfileType.Regular).SupportsBudgetDistribution);
        Assert.True(WorkfileRules.For(WorkfileType.Reefer).SupportsBudgetDistribution);
        Assert.True(WorkfileRules.For(WorkfileType.Trucking).SupportsBudgetDistribution);
    }

    [Fact]
    public void OceanFilesUseOceanContainers_TruckingSideUsesTruckingContainers()
    {
        Assert.Equal(ContainerKind.Ocean, WorkfileRules.For(WorkfileType.Regular).ContainerKind);
        Assert.Equal(ContainerKind.Trucking, WorkfileRules.For(WorkfileType.Reefer).ContainerKind);
        Assert.Equal(ContainerKind.Trucking, WorkfileRules.For(WorkfileType.Trucking).ContainerKind);
    }

    [Fact]
    public void RecordingAGarageMovementOnAnOceanFile_Throws()
    {
        var workfile = RegularWorkfile.Create("x", FreightMode.Import, Guid.NewGuid(), Guid.NewGuid(), Header);

        Assert.Throws<InvalidOperationException>(
            () => workfile.RecordGarageDeparture(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void BillIndicator_IsDroppedOnTypesThatDoNotUseIt()
    {
        // Reefer carries no bill, so an indicator sent by a client must not be stored.
        var reefer = ReeferWorkfile.Create(
            "x", Guid.NewGuid(), null,
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb, BillIndicator = BillIndicator.Hbl });

        Assert.Null(reefer.BillIndicator);
    }

    [Fact]
    public void FactoryBuildsTheSubclassMatchingTheType()
    {
        Assert.IsType<RegularWorkfile>(Create(WorkfileType.Regular));
        Assert.IsType<ReeferWorkfile>(Create(WorkfileType.Reefer));
        Assert.IsType<TruckingWorkfile>(Create(WorkfileType.Trucking));

        static Workfile Create(WorkfileType type) => WorkfileFactory.Create(
            type, FreightMode.Import, TruckingVariant.Truck, "x", Guid.NewGuid(), null, Header);
    }
}
