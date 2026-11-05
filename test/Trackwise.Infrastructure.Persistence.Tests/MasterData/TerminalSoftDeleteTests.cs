using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// Terminal is the first entity to gain <c>ISoftDeletable</c> <i>after</i> it already had callers.
/// Branch was a new table, so the global query filter changed the meaning of nothing; here it
/// changes the meaning of every query already written against terminals, including the ones that
/// render history. These pin both halves - that the filter applies, and that the display lookups
/// which must see retired ports still reach them.
/// </summary>
public class TerminalSoftDeleteTests
{
    [Fact]
    public void OrdinaryTerminalQuery_CarriesTheDeletionPredicate()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<Terminal>().Where(t => t.IsActive).ToQueryString();

        Assert.Contains("deleted_at", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IS NULL", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IgnoreQueryFilters_ReachesRetiredTerminals()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<Terminal>().IgnoreQueryFilters().ToQueryString();

        // What a load that ran through a since-deleted port needs in order to still name it.
        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeletingATerminal_StampsItRatherThanRemovingTheRow()
    {
        await using var db = MasterDataSaveHarness.Create();

        var terminal = NewTerminal();
        db.Add(terminal);
        await db.SaveChangesAsync();

        db.Remove(terminal);
        await db.SaveChangesAsync();

        // The row has to survive: loads and containers still hold its id as a foreign key, and a
        // hard delete would either fail on that constraint or null out their history.
        Assert.NotNull(terminal.DeletedAt);

        // Read off the change tracker rather than re-querying, for the same reason
        // SoftDeleteSaveChangesInterceptorTests does: the in-memory provider cannot materialise the
        // Address complex type. A real delete would leave this entry detached.
        Assert.Equal(EntityState.Unchanged, db.Entry(terminal).State);
    }

    [Fact]
    public void TerminalCodeUniqueness_IsScopedToLiveRows()
    {
        using var db = new TenantDbContext();

        var index = db.Model.FindEntityType(typeof(Terminal))!
            .GetIndexes()
            .Single(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(Terminal.Code)));

        // An unfiltered unique index would let a deleted port hold its UN/LOCODE hostage forever,
        // so the code could never be reissued to the record that replaces it.
        Assert.Equal("deleted_at IS NULL", index.GetFilter());
    }

    private static Terminal NewTerminal() => new()
    {
        Name = "Manila International Container Terminal",
        Code = "PHMNL",
        CountryCode = "PH",
        Type = TerminalType.SeaPort,
        Address = new Address
        {
            StreetAddress = "Eva Macapagal Super Terminal",
            CityOrMunicipality = "Manila",
            Province = "Metro Manila",
            Region = "NCR",
            Zipcode = "1018",
            Country = "PH"
        }
    };
}
