using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// Genset has never been seen booked standalone in the source CCL data - always an add-on to a
/// reefer container job, billed either itemized-by-hour or as a flat profit share when a partner
/// fronts the job. See .claude/handoff/COLD_CHAIN_LOGISTICS_CONTEXT.md §3.
/// </summary>
public class WorkfileGensetAddOnTests
{
    private static readonly WorkfileHeaderData Header = new() { ReportCategory = ReportCategory.Psb };

    private static ReeferWorkfile Reefer() => ReeferWorkfile.Create("x", Guid.NewGuid(), null, Header);

    [Fact]
    public void OnlyReeferFiles_SupportGenset()
    {
        Assert.False(WorkfileRules.For(WorkfileType.Regular).SupportsGenset);
        Assert.False(WorkfileRules.For(WorkfileType.Trucking).SupportsGenset);
        Assert.True(WorkfileRules.For(WorkfileType.Reefer).SupportsGenset);
    }

    [Fact]
    public void SettingAGensetAddOnOnANonReeferFile_Throws()
    {
        var workfile = RegularWorkfile.Create("x", FreightMode.Import, Guid.NewGuid(), Guid.NewGuid(), Header);

        Assert.Throws<InvalidOperationException>(
            () => workfile.SetGensetAddOn(null, GensetBillingType.ProfitShare, null, 2000m, null));
    }

    [Fact]
    public void Itemized_ComputesTotalChargeFromRateTimesHoursUsed()
    {
        var workfile = Reefer();
        var gensetId = Guid.NewGuid();

        workfile.SetGensetAddOn(gensetId, GensetBillingType.Itemized, 350m, null, "2 units on standby");

        Assert.Equal(gensetId, workfile.GensetId);
        Assert.Null(workfile.GensetTotalCharge); // no hours logged yet

        workfile.RecordGensetHoursUsed(6m);

        Assert.Equal(6m, workfile.GensetHoursUsed);
        Assert.Equal(2100m, workfile.GensetTotalCharge);
    }

    [Fact]
    public void ProfitShare_TotalChargeIsTheFlatAmountRegardlessOfHours()
    {
        var workfile = Reefer();

        workfile.SetGensetAddOn(null, GensetBillingType.ProfitShare, null, 2000m, "SULONG fronting the job");

        Assert.Equal(2000m, workfile.GensetTotalCharge);
    }

    [Fact]
    public void Itemized_WithNoHourlyRate_Throws()
    {
        var workfile = Reefer();

        Assert.Throws<InvalidOperationException>(
            () => workfile.SetGensetAddOn(null, GensetBillingType.Itemized, null, null, null));
    }

    [Fact]
    public void ProfitShare_WithNoFlatAmount_Throws()
    {
        var workfile = Reefer();

        Assert.Throws<InvalidOperationException>(
            () => workfile.SetGensetAddOn(null, GensetBillingType.ProfitShare, null, null, null));
    }

    [Fact]
    public void RecordingHoursAgainstAProfitShareAddOn_Throws()
    {
        var workfile = Reefer();
        workfile.SetGensetAddOn(null, GensetBillingType.ProfitShare, null, 2000m, null);

        Assert.Throws<InvalidOperationException>(() => workfile.RecordGensetHoursUsed(3m));
    }

    [Fact]
    public void RecordingHoursWithNoAddOnAtAll_Throws()
    {
        var workfile = Reefer();

        Assert.Throws<InvalidOperationException>(() => workfile.RecordGensetHoursUsed(3m));
    }

    [Fact]
    public void SwitchingBillingType_DiscardsHoursLoggedUnderTheOldShape()
    {
        var workfile = Reefer();
        workfile.SetGensetAddOn(null, GensetBillingType.Itemized, 350m, null, null);
        workfile.RecordGensetHoursUsed(4m);

        workfile.SetGensetAddOn(null, GensetBillingType.ProfitShare, null, 2000m, null);

        Assert.Null(workfile.GensetHoursUsed);
        Assert.Equal(2000m, workfile.GensetTotalCharge);
    }

    [Fact]
    public void EditingTheSameBillingType_LeavesRecordedHoursAlone()
    {
        var workfile = Reefer();
        workfile.SetGensetAddOn(null, GensetBillingType.Itemized, 350m, null, null);
        workfile.RecordGensetHoursUsed(4m);

        workfile.SetGensetAddOn(null, GensetBillingType.Itemized, 400m, null, "rate corrected");

        Assert.Equal(4m, workfile.GensetHoursUsed);
        Assert.Equal(1600m, workfile.GensetTotalCharge);
    }

    [Fact]
    public void RemovingTheAddOn_ClearsEveryField()
    {
        var workfile = Reefer();
        workfile.SetGensetAddOn(Guid.NewGuid(), GensetBillingType.Itemized, 350m, null, "note");
        workfile.RecordGensetHoursUsed(2m);

        workfile.RemoveGensetAddOn();

        Assert.Null(workfile.GensetId);
        Assert.Null(workfile.GensetBillingType);
        Assert.Null(workfile.GensetHourlyRate);
        Assert.Null(workfile.GensetHoursUsed);
        Assert.Null(workfile.GensetFlatAmount);
        Assert.Null(workfile.GensetNotes);
        Assert.Null(workfile.GensetTotalCharge);
    }

    [Fact]
    public void RemovingWithNoAddOnPresent_Throws()
    {
        var workfile = Reefer();

        Assert.Throws<InvalidOperationException>(() => workfile.RemoveGensetAddOn());
    }

    [Fact]
    public void AGensetAddOnCannotBeChangedOnceTheFileLeavesOpen()
    {
        var workfile = Reefer();
        workfile.SetGensetAddOn(null, GensetBillingType.ProfitShare, null, 2000m, null);
        workfile.SubmitForApproval();

        Assert.Throws<InvalidOperationException>(
            () => workfile.SetGensetAddOn(null, GensetBillingType.Itemized, 100m, null, null));
    }
}
