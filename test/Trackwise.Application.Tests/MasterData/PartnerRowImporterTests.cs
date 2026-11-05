using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The partner importer adds two things to the reference-list shape: a column that holds a list
/// rather than a value, and an address block that is optional as a whole but not field by field.
/// </summary>
public class PartnerRowImporterTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Branch, Guid> branches =
        Substitute.For<ITenantRepository<Branch, Guid>>();

    private readonly Guid manilaId = Guid.NewGuid();

    public PartnerRowImporterTests()
    {
        tenantUow.Repository<Branch>().Returns(branches);
        branches.Query().Returns(_ => new List<Branch>
        {
            new() { Id = manilaId, Code = "MNL", Name = "Manila Head Office" }
        }.BuildMock());
    }

    private static MasterDataImportRow Row(params (string Column, string Value)[] values) =>
        new(2, values.ToDictionary(
            v => MasterDataImportHeader.Normalize(v.Column),
            v => (string?)v.Value));

    private static MasterDataImportRow ValidRow(params (string Column, string Value)[] overrides)
    {
        var values = new (string, string)[]
        {
            ("Name", "Maersk Filipinas Inc."),
            ("Roles", "ShippingLine")
        };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private Task<MasterDataRowBinding> Bind(MasterDataImportRow row) =>
        new PartnerRowImporter(tenantUow).BindAsync(row, MasterDataImportOverride.None);

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow(
            ("TIN", "123-456-789-000"),
            ("ContactName", "Ana Reyes"),
            ("Branch", "mnl")));

        var command = Assert.IsType<CreatePartnerCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal("Maersk Filipinas Inc.", command.Name);
        Assert.Equal([BusinessPartnerRole.ShippingLine], command.Roles);
        Assert.Equal(manilaId, command.BranchId);

        // Passed through raw: the command's own property normalises it, so the file and the form
        // reach the database by exactly the same route.
        Assert.Equal("123-456-789-000", command.TaxId);
    }

    [Fact]
    public async Task ImportedRowsAreDraftsLikeAnyOtherCreate()
    {
        // The largest hole maker-checker could have: a file that published four hundred partners
        // without anybody looking at them. The importer sends the ordinary create command, which
        // produces a draft, so the file is a faster way to type rather than a way round the second
        // person. Pinned here because nothing in the importer itself says so.
        var binding = await Bind(ValidRow());

        Assert.IsType<CreatePartnerCommand>(binding.Command);
        Assert.DoesNotContain(
            typeof(CreatePartnerCommand).GetProperties(),
            property => property.Name is "IsActive" or "ApprovalState");
    }

    [Fact]
    public async Task SeveralRoles_AreSplitOnSpacesAndCommas()
    {
        // A company acts in several capacities at once, so the column is a list. Space-separated
        // because the delimiter sniffing already claims commas - a file that arrives comma-delimited
        // would otherwise split the row rather than the cell.
        var binding = await Bind(ValidRow(("Roles", "ShippingLine Agent")));

        var command = Assert.IsType<CreatePartnerCommand>(binding.Command);
        Assert.Equal(
            [BusinessPartnerRole.ShippingLine, BusinessPartnerRole.Agent],
            command.Roles);
    }

    [Fact]
    public async Task RepeatedRoles_AreCollapsed()
    {
        var binding = await Bind(ValidRow(("Roles", "trucker TRUCKER Depot")));

        var command = Assert.IsType<CreatePartnerCommand>(binding.Command);
        Assert.Equal([BusinessPartnerRole.Trucker, BusinessPartnerRole.Depot], command.Roles);
    }

    [Fact]
    public async Task AWordThatIsNotARole_IsRejectedByName()
    {
        var binding = await Bind(ValidRow(("Roles", "ShippingLine Haulier")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Roles", issue.Column);
        Assert.Contains("Haulier", issue.Message);
        Assert.Equal(2, issue.Line);
    }

    [Fact]
    public async Task ABlankRolesColumn_IsLeftToTheValidator()
    {
        // Shape and lookup failures belong to the importer; field rules belong to the command's
        // validator. Restating "a partner must have at least one role" here is how a file and a
        // form start disagreeing about what is legal.
        var binding = await Bind(Row(("Name", "Acme Brokerage")));

        var command = Assert.IsType<CreatePartnerCommand>(binding.Command);
        Assert.Empty(command.Roles);
        Assert.Empty(binding.Issues);
    }

    [Fact]
    public async Task NoAddressColumns_LeaveTheAddressNull()
    {
        // A broker recorded by name and TIN alone is a legitimate record. An address with every
        // field blank is not an empty address - it reads later as a fact somebody established.
        var binding = await Bind(ValidRow());

        var command = Assert.IsType<CreatePartnerCommand>(binding.Command);
        Assert.Null(command.Address);
    }

    [Fact]
    public async Task OneAddressColumn_BuildsTheWholeBlockSoTheValidatorCanNameWhatIsMissing()
    {
        var binding = await Bind(ValidRow(("City", "Taguig")));

        var command = Assert.IsType<CreatePartnerCommand>(binding.Command);
        Assert.NotNull(command.Address);
        Assert.Equal("Taguig", command.Address!.CityOrMunicipality);
        Assert.Equal("", command.Address.StreetAddress);
        Assert.Empty(binding.Issues);
    }

    [Fact]
    public async Task AnUnknownBranchCode_IsRejectedRatherThanImportedGlobally()
    {
        // "Applies everywhere" is a decision, not a fallback for a typo.
        var binding = await Bind(ValidRow(("Branch", "XYZ")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Branch", issue.Column);
    }

    [Fact]
    public void EveryColumnTheImporterReads_IsDeclaredInTheTemplate()
    {
        // The template endpoint is generated from this declaration, so a column read but not
        // declared is one a steward is never told to supply - and the header match is the only
        // thing standing between them and a silently ignored value.
        var declared = new PartnerRowImporter(tenantUow).Columns
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var column in new[]
                 {
                     "Code", "Name", "Roles", "TIN", "ContactName", "Email", "Phone",
                     "Street", "Subdivision", "Barangay", "City", "Province", "Region",
                     "Zipcode", "Country", "Branch", "Notes"
                 })
        {
            Assert.Contains(column, declared);
        }
    }
}
