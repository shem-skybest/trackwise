using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// Truck is the second entity (after Terminal) to gain <c>ISoftDeletable</c> after it already had
/// ~59 Application-layer call sites. These pin the same two halves Terminal's promotion needed: the
/// filter applies to an ordinary query, and the display lookups that must still see a retired truck
/// - a delivered load naming the unit that hauled it - can reach past it.
/// </summary>
public class TruckSoftDeleteTests
{
    [Fact]
    public void OrdinaryTruckQuery_CarriesTheDeletionPredicate()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<Truck>().Where(t => t.IsActive).ToQueryString();

        Assert.Contains("deleted_at", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IS NULL", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IgnoreQueryFilters_ReachesRetiredTrucks()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<Truck>().IgnoreQueryFilters().ToQueryString();

        // What a load, trip or maintenance record that referenced a since-deleted truck needs in
        // order to still display its unit number.
        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeletingATruck_StampsItRatherThanRemovingTheRow()
    {
        await using var db = MasterDataSaveHarness.Create();

        var truck = NewTruck();
        db.Add(truck);
        await db.SaveChangesAsync();

        db.Remove(truck);
        await db.SaveChangesAsync();

        // The row has to survive: loads, trips and expenses still hold its id as a foreign key, and
        // a hard delete would either fail on that constraint or null out their history.
        Assert.NotNull(truck.DeletedAt);

        // Read off the change tracker rather than re-querying - the in-memory provider cannot
        // materialise the entity's complex properties. A real delete would leave this entry detached.
        Assert.Equal(EntityState.Unchanged, db.Entry(truck).State);
    }

    [Fact]
    public void TruckNumberUniqueness_IsScopedToLiveRows()
    {
        using var db = new TenantDbContext();

        var index = db.Model.FindEntityType(typeof(Truck))!
            .GetIndexes()
            .Single(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(Truck.Number)));

        // An unfiltered unique index would let a deleted truck hold its unit number hostage forever,
        // so the number could never be reissued to the unit that replaces it.
        Assert.Equal("deleted_at IS NULL", index.GetFilter());
    }

    [Fact]
    public void TruckVinUniqueness_IsScopedToLiveRowsWherePresent()
    {
        using var db = new TenantDbContext();

        var index = db.Model.FindEntityType(typeof(Truck))!
            .GetIndexes()
            .Single(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(Truck.Vin)));

        // Most legacy trucks have no VIN on file, so the index has to exclude both deleted rows and
        // the (common) blank VIN, not just one of the two.
        Assert.Equal("deleted_at IS NULL AND vin IS NOT NULL", index.GetFilter());
    }

    private static Truck NewTruck() => new()
    {
        Number = "TRK-042",
        Name = "2022 Freightliner Cascadia",
        Type = TruckType.FreightTruck
    };
}
