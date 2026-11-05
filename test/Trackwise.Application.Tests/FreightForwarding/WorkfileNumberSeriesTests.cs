using Trackwise.Domain.Entities;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// BKN-RFV continues Skybest's nSFS numbering, so it must read and restart the way nSFS does: per
/// year and branch, four digits, the year the booking was entered. The other series are untouched.
/// </summary>
public class WorkfileNumberSeriesTests
{
    private static readonly DateOnly Oct2026 = new(2026, 10, 5);

    [Fact]
    public void Reefer_FormatsAsNsfsDoes()
    {
        Assert.Equal("BKN-RFV1514-26-M", WorkfileNumberSeries.FormatReference("BKN-RFV", Oct2026, 1514));
        Assert.Equal("BKN-RFV0001-26-M", WorkfileNumberSeries.FormatReference("BKN-RFV", Oct2026, 1));
        Assert.Equal("BKN-RFV12345-26-M", WorkfileNumberSeries.FormatReference("BKN-RFV", Oct2026, 12345));
    }

    [Fact]
    public void Reefer_HasACounterPerYear_SoJanuaryStartsAtOne()
    {
        Assert.Equal("BKN-RFV-26-M", WorkfileNumberSeries.CounterKey("BKN-RFV", new DateOnly(2026, 12, 31)));
        Assert.Equal("BKN-RFV-27-M", WorkfileNumberSeries.CounterKey("BKN-RFV", new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void ReeferBilling_NumbersLikeBknRfv_OnItsOwnCounter()
    {
        Assert.Equal("SLC-RFV0258-26-M", WorkfileNumberSeries.FormatReference("SLC-RFV", Oct2026, 258));
        Assert.Equal("SLC-RFV-26-M", WorkfileNumberSeries.CounterKey("SLC-RFV", Oct2026));
    }

    [Fact]
    public void TruckLeg_ContinuesNsfsSlcTrk_OnItsOwnCounter()
    {
        Assert.Equal("SLC-TRK1390-26-M", WorkfileNumberSeries.FormatReference(WorkfileSeries.TruckLeg, Oct2026, 1390));
        Assert.Equal("SLC-TRK-26-M", WorkfileNumberSeries.CounterKey(WorkfileSeries.TruckLeg, Oct2026));
    }

    [Theory]
    [InlineData("BKN-IMP")]
    [InlineData("BKN-TRK")]
    public void OtherSeries_KeepOneCounterAndTheirFormat(string series)
    {
        Assert.Equal(series, WorkfileNumberSeries.CounterKey(series, Oct2026));
        Assert.Equal($"{series}-000123", WorkfileNumberSeries.FormatReference(series, Oct2026, 123));
    }
}
