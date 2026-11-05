using Trackwise.Domain.Entities;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// While nSFS still issues Cold Chain booking numbers, staff type them in as nSFS or Monday shows
/// them; TrackWise stores nSFS's printed form so the same booking always matches.
/// </summary>
public class ReeferBookingNumberTests
{
    private static readonly DateOnly Today = new(2026, 10, 7);

    [Theory]
    [InlineData("BKN-RFV1331-26-M", "BKN-RFV1331-26-M")]
    [InlineData("bkn-rfv1331-26-m", "BKN-RFV1331-26-M")]
    [InlineData("BKN-RFV 1331", "BKN-RFV1331-26-M")]
    [InlineData("1331", "BKN-RFV1331-26-M")]
    [InlineData(" 42 ", "BKN-RFV0042-26-M")]
    [InlineData("BKN-RFV2146-25-M", "BKN-RFV2146-25-M")]
    [InlineData("BKN-RFV0310-26-C", "BKN-RFV0310-26-C")]
    public void Normalize_AsStaffTypeIt_GivesNsfsPrintedForm(string typed, string expected)
    {
        Assert.Equal(expected, ReeferBookingNumber.Normalize(typed, Today));
    }

    [Theory]
    [InlineData("SLC-RFV0257-26-M")]
    [InlineData("BKN-IMP-000123")]
    [InlineData("0")]
    [InlineData("abc")]
    public void Normalize_NotABookingNumber_Throws(string typed)
    {
        Assert.Throws<InvalidOperationException>(() => ReeferBookingNumber.Normalize(typed, Today));
    }
}
