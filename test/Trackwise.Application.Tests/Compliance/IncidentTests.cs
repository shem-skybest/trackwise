using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Primitives.Enums.Safety;
using Xunit;

namespace Trackwise.Application.Tests.Compliance;

public class IncidentTests
{
    private static readonly DateTime Now = new(2026, 4, 11, 12, 0, 0, DateTimeKind.Utc);
    private static readonly IncidentParties NoParties = new(null, null, null, null, null, null);

    private static IncidentDetails Accident() => new()
    {
        Kind = IncidentKind.Accident,
        Severity = IncidentSeverity.Minor,
        OccurredAt = Now.AddHours(-10),
        Summary = "Windshield broken on the way to drop 3; reported at the police station.",
        PoliceReportFiled = true,
        PoliceStation = "Caloocan",
        BarangayBlotterNumber = "BB-0411"
    };

    #region Recording

    [Fact]
    public void Record_KeepsTheCaseNumberAndPrevention()
    {
        var incident = Incident.Record(
            " CCL-CASENO.081 ", Accident() with { Prevention = "Check P.O. validity before dispatch " }, NoParties, Now);

        Assert.Equal("CCL-CASENO.081", incident.CaseNumber);
        Assert.Equal("Check P.O. validity before dispatch", incident.Prevention);
    }

    [Fact]
    public void Record_WithoutCaseNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Incident.Record(" ", Accident(), NoParties, Now));
    }

    [Fact]
    public void Record_Accident_IsOpenAndKeepsAnUnknownTypeUnknown()
    {
        var incident = Incident.Record("CCL-CASENO.081", Accident(), NoParties, Now);

        Assert.Equal(IncidentStatus.Open, incident.Status);
        Assert.Null(incident.AccidentType);
        Assert.Equal("BB-0411", incident.BarangayBlotterNumber);
    }

    [Fact]
    public void Record_NotAnAccident_DropsTheAccidentType()
    {
        var details = Accident() with { Kind = IncidentKind.Breakdown, AccidentType = AccidentType.Collision };

        var incident = Incident.Record("CCL-CASENO.081", details, NoParties, Now);

        Assert.Null(incident.AccidentType);
    }

    [Fact]
    public void Record_InTheFuture_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Incident.Record("CCL-CASENO.081", Accident() with { OccurredAt = Now.AddHours(1) }, NoParties, Now));
    }

    [Fact]
    public void Record_InjuriesWithoutACount_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Incident.Record("CCL-CASENO.081", Accident() with { AnyInjuries = true }, NoParties, Now));
    }

    [Fact]
    public void Record_NoPoliceReport_ClearsItsNumber()
    {
        var details = Accident() with { PoliceReportFiled = false, PoliceReportNumber = "PR-1" };

        var incident = Incident.Record("CCL-CASENO.081", details, NoParties, Now);

        Assert.Null(incident.PoliceReportNumber);
        Assert.Null(incident.PoliceStation);
    }

    [Fact]
    public void Record_SameHelperTwice_Throws()
    {
        var helper = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() =>
            Incident.Record("CCL-CASENO.081", Accident(), NoParties with { HelperId = helper, SecondHelperId = helper }, Now));
    }

    #endregion

    #region Status

    [Fact]
    public void Resolve_WithoutCorrectiveAction_Throws()
    {
        var incident = Incident.Record("CCL-CASENO.081", Accident(), NoParties, Now);

        Assert.Throws<InvalidOperationException>(() => incident.Resolve("  ", "ops", Now));
    }

    [Fact]
    public void Resolve_ThenEdit_Throws()
    {
        var incident = Incident.Record("CCL-CASENO.081", Accident(), NoParties, Now);
        incident.Resolve("Windshield replaced (RFV26-001385-M); driver briefed.", "ops", Now);

        Assert.Throws<InvalidOperationException>(() => incident.Edit(Accident(), NoParties, Now));
    }

    [Fact]
    public void Reopen_ClearsTheResolutionAndAllowsEditing()
    {
        var incident = Incident.Record("CCL-CASENO.081", Accident(), NoParties, Now);
        incident.Resolve("Windshield replaced.", "ops", Now);

        incident.Reopen("Client asked about the delay");
        incident.Edit(Accident() with { Severity = IncidentSeverity.Moderate }, NoParties, Now);

        Assert.Equal(IncidentStatus.Open, incident.Status);
        Assert.Null(incident.ResolvedAt);
        Assert.Equal(IncidentSeverity.Moderate, incident.Severity);
    }

    [Fact]
    public void Close_BeforeResolving_Throws()
    {
        var incident = Incident.Record("CCL-CASENO.081", Accident(), NoParties, Now);

        Assert.Throws<InvalidOperationException>(() => incident.Close("ops", Now));
    }

    [Fact]
    public void EnsureDeletable_AfterReview_Throws()
    {
        var incident = Incident.Record("CCL-CASENO.081", Accident(), NoParties, Now);
        incident.StartReview("Checking who pays", "ops", Now);

        Assert.Throws<InvalidOperationException>(() => incident.EnsureDeletable());
    }

    #endregion
}
