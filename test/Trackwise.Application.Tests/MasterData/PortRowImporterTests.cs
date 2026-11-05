using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The per-entity half of an import resolves and rejects; it never validates. These pin that
/// division - a lookup a file can get wrong is caught here and named by column, while every field
/// rule is left to <c>CreatePortValidator</c>, so a file and the steward's form cannot drift into
/// disagreeing about what a legal port is.
/// </summary>
public class PortRowImporterTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Branch, Guid> branches =
        Substitute.For<ITenantRepository<Branch, Guid>>();

    private readonly Guid manilaId = Guid.NewGuid();

    public PortRowImporterTests()
    {
        tenantUow.Repository<Branch>().Returns(branches);
        branches.Query().Returns(_ => new List<Branch> { Manila() }.BuildMock());
    }

    private Branch Manila() => new()
    {
        Id = manilaId,
        Code = "MNL",
        Name = "Manila Head Office"
    };

    private static MasterDataImportRow Row(params (string Column, string Value)[] values) =>
        new(2, values.ToDictionary(
            v => MasterDataImportHeader.Normalize(v.Column),
            v => (string?)v.Value));

    private static MasterDataImportRow ValidRow(params (string Column, string Value)[] overrides)
    {
        var values = new (string, string)[]
        {
            ("Code", "PHMNL"),
            ("Name", "Manila International Container Terminal"),
            ("Country", "ph"),
            ("Type", "SeaPort"),
            ("Street", "South Access Road"),
            ("City", "Manila"),
            ("Province", "Metro Manila"),
            ("Region", "NCR"),
            ("Zipcode", "1018")
        };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private Task<MasterDataRowBinding> Bind(
        MasterDataImportRow row, MasterDataImportOverride? overrides = null) =>
        new PortRowImporter(tenantUow).BindAsync(row, overrides ?? MasterDataImportOverride.None);

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow());

        var command = Assert.IsType<CreatePortCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal("PHMNL", command.Code);
        Assert.Equal(TerminalType.SeaPort, command.Type);
        Assert.Equal("Manila", command.Address.CityOrMunicipality);
    }

    [Fact]
    public async Task CountryIsUpperCasedAndDoublesAsTheAddressCountry()
    {
        // Asking for the country twice invites the two answers to disagree, and a port in a
        // different country from its own address is not a thing.
        var binding = await Bind(ValidRow(("Country", "ph")));

        var command = (CreatePortCommand)binding.Command!;
        Assert.Equal("PH", command.CountryCode);
        Assert.Equal("PH", command.Address.Country);
    }

    [Theory]
    [InlineData("seaport")]
    [InlineData("SEAPORT")]
    [InlineData("SeaPort")]
    public async Task TypeIsReadWithoutRegardToCase(string type)
    {
        var binding = await Bind(ValidRow(("Type", type)));

        Assert.Equal(TerminalType.SeaPort, ((CreatePortCommand)binding.Command!).Type);
    }

    [Fact]
    public async Task UnknownType_IsRejectedAgainstItsColumnAndListsTheOptions()
    {
        var binding = await Bind(ValidRow(("Type", "Wharf")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Type", issue.Column);
        Assert.Equal(2, issue.Line);
        Assert.Contains(nameof(TerminalType.InlandDepot), issue.Message);
    }

    [Fact]
    public async Task BranchCodeResolvesToItsId()
    {
        var binding = await Bind(ValidRow(("Branch", "mnl")));

        Assert.Equal(manilaId, ((CreatePortCommand)binding.Command!).BranchId);
    }

    [Fact]
    public async Task NoBranch_MeansEveryBranch()
    {
        var binding = await Bind(ValidRow());

        Assert.Null(((CreatePortCommand)binding.Command!).BranchId);
    }

    [Fact]
    public async Task UnknownBranch_IsRejectedRatherThanImportedAsGlobal()
    {
        // "Applies everywhere" is a decision a steward makes, not a fallback for a typo. Silently
        // widening the scope is the kind of error nobody finds until a second office edits a record
        // it does not own.
        var binding = await Bind(ValidRow(("Branch", "XXX")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Branch", issue.Column);
        Assert.Contains("XXX", issue.Message);
    }

    [Fact]
    public async Task RepeatedBranchCodeIsLookedUpOnce()
    {
        // A file scoped to one office is the normal case, so without the cache this is one query
        // per row for the same answer.
        var importer = new PortRowImporter(tenantUow);

        await importer.BindAsync(ValidRow(("Branch", "MNL")), MasterDataImportOverride.None);
        await importer.BindAsync(ValidRow(("Branch", "MNL")), MasterDataImportOverride.None);

        branches.Received(1).Query();
    }

    [Fact]
    public async Task BlankCode_IsLeftForTheHandlerToAllocate()
    {
        var binding = await Bind(ValidRow(("Code", "")));

        Assert.Null(((CreatePortCommand)binding.Command!).Code);
    }

    [Fact]
    public async Task MissingRequiredValue_IsPassedThroughForTheValidatorToRefuse()
    {
        // Deliberately not caught here. Restating the field rules would give the file and the form
        // two sets of rules to keep in step, and they would not stay in step.
        var binding = await Bind(ValidRow(("Name", "")));

        Assert.NotNull(binding.Command);
        Assert.Equal("", ((CreatePortCommand)binding.Command!).Name);
    }

    [Fact]
    public async Task OverrideIsCarriedOntoTheCommand()
    {
        var binding = await Bind(
            ValidRow(), new MasterDataImportOverride(ConfirmDistinct: true, Reason: "New terminal"));

        var command = (CreatePortCommand)binding.Command!;
        Assert.True(command.ConfirmedDistinct);
        Assert.Equal("New terminal", command.DistinctReason);
    }

    [Fact]
    public void EveryDeclaredColumnCarriesAnExplanation()
    {
        // The template is the only place a steward is told what the columns mean, and it is built
        // straight from these.
        var columns = new PortRowImporter(tenantUow).Columns;

        Assert.All(columns, c => Assert.False(string.IsNullOrWhiteSpace(c.Description)));
        Assert.Contains(columns, c => c is { Name: "Name", Required: true });
    }
}
