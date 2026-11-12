using Trackwise.Application.Modules.Operations.FreightForwarding.Services.BookingRequests;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// Reading the AI's reply to a client's booking request: proposals may only point at locations the
/// model was shown, and a name saved word for word is matched even when the model held back.
/// </summary>
public class BookingRequestReplyParserTests
{
    private static readonly ProposedSiteDto EdsaShang = Site("EDSA SHANG");
    private static readonly ProposedSiteDto EdshaShangriLa = Site("EDSHA SHANGRI-LA");
    private static readonly ProposedSiteDto TheWestin = Site("THE WESTIN");
    private static readonly ProposedSiteDto WestinHotel = Site("WESTIN HOTEL");
    private static readonly ProposedSiteDto SohoGrandWestside = Site("SOHO/GRANDWESTSIDE");
    private static readonly ProposedSiteDto Warehouse = Site("Global San Pablo");

    private static readonly Dictionary<string, ProposedSiteDto> Sites = new()
    {
        ["S1"] = EdsaShang,
        ["S2"] = EdshaShangriLa,
        ["S3"] = TheWestin,
        ["S4"] = WestinHotel,
        ["S5"] = SohoGrandWestside,
        ["S6"] = Warehouse
    };

    [Fact]
    public void Parse_TwoTrucks_ReadsEachTruckPlan()
    {
        const string reply = """
            {"trips":[
              {"serviceDate":"2026-09-29","callTime":"07:00","truckSize":"4T","pickupSiteKey":"S6",
               "pickupText":"vfel","commodity":"frozen banana","temperatureMinC":-18,"temperatureMaxC":-15,
               "notes":"Need health cert","drops":[{"text":"westin","siteKey":"S3","boxes":200}]},
              {"serviceDate":"2026-09-29","truckSize":"2T","drops":[]}
            ],"warnings":[]}
            """;

        var result = BookingRequestReplyParser.Parse(reply, Sites);

        Assert.True(result.IsSuccess);
        var trips = result.Value!.Trips;
        Assert.Equal(2, trips.Count);
        Assert.Equal(new DateOnly(2026, 9, 29), trips[0].ServiceDate);
        Assert.Equal(new TimeOnly(7, 0), trips[0].CallTime);
        Assert.Equal(TruckTonnageClass.FourTonner, trips[0].TruckSize);
        Assert.Equal(Warehouse.Id, trips[0].PickupSite!.Id);
        Assert.Equal(-18m, trips[0].TemperatureMinC);
        Assert.Equal(TheWestin.Id, Assert.Single(trips[0].Drops).Site!.Id);
        Assert.Equal(200, trips[0].Drops[0].PlannedBoxes);
        Assert.Equal(TruckTonnageClass.TwoTonner, trips[1].TruckSize);
    }

    [Fact]
    public void Parse_DropSavedWordForWord_MatchesItEvenWhenModelHeldBack()
    {
        const string reply = """
            {"trips":[{"drops":[
              {"text":"edsa shang","siteKey":null,"candidateKeys":["S1","S2"]},
              {"text":"soho/grandwestside","siteKey":null,"candidateKeys":[]}
            ]}]}
            """;

        var drops = BookingRequestReplyParser.Parse(reply, Sites).Value!.Trips[0].Drops;

        Assert.Equal(EdsaShang.Id, drops[0].Site!.Id);
        Assert.Empty(drops[0].Candidates);
        Assert.Equal(SohoGrandWestside.Id, drops[1].Site!.Id);
    }

    [Fact]
    public void Parse_AmbiguousDrop_LeavesItOpenWithKnownCandidates()
    {
        const string reply = """
            {"trips":[{"drops":[{"text":"westin","siteKey":null,"candidateKeys":["S3","S4","S99","S3"]}]}]}
            """;

        var drop = BookingRequestReplyParser.Parse(reply, Sites).Value!.Trips[0].Drops[0];

        Assert.Null(drop.Site);
        Assert.Equal([TheWestin.Id, WestinHotel.Id], drop.Candidates.Select(c => c.Id));
    }

    [Fact]
    public void Parse_UnknownSiteKey_DropsItAndWarns()
    {
        const string reply = """
            {"trips":[{"pickupSiteKey":"S42","pickupText":"Calauan","drops":[{"text":"laguna","siteKey":"S77"}]}]}
            """;

        var reading = BookingRequestReplyParser.Parse(reply, Sites).Value!;

        Assert.Null(reading.Trips[0].PickupSite);
        Assert.Equal("Calauan", reading.Trips[0].PickupText);
        Assert.Null(reading.Trips[0].Drops[0].Site);
        Assert.Equal(2, reading.Warnings.Count);
    }

    [Theory]
    [InlineData("3T", TruckTonnageClass.ThreeTonner)]
    [InlineData("10t", TruckTonnageClass.TenTonner)]
    [InlineData("4 tonner", null)]
    [InlineData(null, null)]
    public void Parse_TruckSize_ReadsOnlyAllowedCodes(string? size, TruckTonnageClass? expected)
    {
        var reply = $$"""{"trips":[{"truckSize":{{(size is null ? "null" : $"\"{size}\"")}}}]}""";

        Assert.Equal(expected, BookingRequestReplyParser.Parse(reply, Sites).Value!.Trips[0].TruckSize);
    }

    [Fact]
    public void Parse_FencedReplyWithLooseValues_ReadsWhatItCan()
    {
        const string reply = """
            Here you go:
            ```json
            {"trips":[{"serviceDate":"Sept 29","callTime":"4am","drops":[{"text":"Baguio","boxes":"12"}]}]}
            ```
            """;

        var trip = BookingRequestReplyParser.Parse(reply, Sites).Value!.Trips[0];

        Assert.Null(trip.ServiceDate);
        Assert.Null(trip.CallTime);
        Assert.Equal(12, trip.Drops[0].PlannedBoxes);
    }

    [Fact]
    public void Parse_NotJson_FailsReadably()
    {
        var result = BookingRequestReplyParser.Parse("Sorry, I can't read this image.", Sites);

        Assert.False(result.IsSuccess);
        Assert.Contains("by hand", result.Error);
    }

    private static ProposedSiteDto Site(string name) => new() { Id = Guid.NewGuid(), Name = name };
}
