using Trackwise.Domain.Entities;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

public class NumberSeriesTests
{
    [Theory]
    [InlineData("BR", "-", 6, 1, "BR-000001")]
    [InlineData("BR", "-", 6, 123456, "BR-123456")]
    [InlineData("BR", "", 4, 7, "BR0007")]
    [InlineData("", "-", 6, 42, "000042")]
    public void Format_AppliesTheSeriesPattern(
        string prefix, string separator, int padWidth, long value, string expected)
    {
        var series = new NumberSeries
        {
            Series = "Branch", Prefix = prefix, Separator = separator, PadWidth = padWidth
        };

        Assert.Equal(expected, series.Format(value));
    }

    [Fact]
    public void Format_DoesNotTruncateANumberWiderThanThePadding()
    {
        var series = new NumberSeries { Series = "Branch", Prefix = "BR", PadWidth = 3 };

        // Running past the configured width must keep counting, not wrap or clip - the code still
        // has to be unique.
        Assert.Equal("BR-12345", series.Format(12345));
    }
}

public class MasterDataXrefTests
{
    [Fact]
    public void BuildKey_TrimsEachPart()
    {
        // Legacy columns are riddled with padding - nSFS SQL is full of rtrim(ltrim(...)) for
        // exactly this reason. The same logical key must not land twice under two spellings.
        Assert.Equal("MNL|01", MasterDataXref.BuildKey("  MNL ", "01  "));
    }

    [Fact]
    public void BuildKey_TreatsNullAndEmptyAlike()
    {
        Assert.Equal(MasterDataXref.BuildKey("MNL", ""), MasterDataXref.BuildKey("MNL", null));
    }

    [Fact]
    public void BuildKey_IsStableForASinglePart()
    {
        Assert.Equal("TBL123", MasterDataXref.BuildKey(" TBL123 "));
    }
}
