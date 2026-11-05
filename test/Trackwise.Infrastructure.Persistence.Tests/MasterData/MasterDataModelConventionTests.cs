using Trackwise.Domain.Core;
using Trackwise.Domain.Entities;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// The master-data conventions are applied by reflection over marker interfaces, so a new entity
/// picks them up silently - and loses them just as silently if a convention stops matching. Each of
/// these failures is invisible at runtime until it matters: a missing query filter leaks deleted
/// records into every list, a mapped <c>Group</c> adds a redundant column, and an unfiltered unique
/// index makes a code unreusable forever.
/// </summary>
public class MasterDataModelConventionTests
{
    [Fact]
    public void SoftDeletedRows_AreExcludedFromTheSqlOfAnOrdinaryQuery()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<Branch>().Where(b => b.IsActive).ToQueryString();

        // The whole point of the global filter: a query that never mentions deletion still cannot
        // see deleted records, so no list screen can leak them by forgetting a Where clause.
        Assert.Contains("deleted_at", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IS NULL", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IgnoreQueryFilters_DropsTheDeletionPredicate()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<Branch>().IgnoreQueryFilters().ToQueryString();

        // Stewards reviewing what was deleted need an escape hatch, and it has to actually work.
        // The column still appears in the projection - it is the predicate that must be gone.
        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EverySoftDeletableEntityInTheModel_HasAQueryFilter()
    {
        using var db = new TenantDbContext();

        var unfiltered = db.Model.GetEntityTypes()
            .Where(e => typeof(ISoftDeletable).IsAssignableFrom(e.ClrType)
                        && e.BaseType is null
                        && !e.IsOwned()
                        && e.GetDeclaredQueryFilters().Count == 0)
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.Empty(unfiltered);
    }

    [Fact]
    public void MasterDataGroup_IsNotPersisted()
    {
        using var db = new TenantDbContext();

        var entityType = db.Model.FindEntityType(typeof(Branch))!;

        Assert.Null(entityType.FindProperty(nameof(IMasterDataRecord.Group)));
    }

    [Fact]
    public void CodeUniqueness_IsScopedToLiveRows()
    {
        using var db = new TenantDbContext();

        var index = db.Model.FindEntityType(typeof(Branch))!
            .GetIndexes()
            .Single(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(Branch.Code)));

        // Without the filter a soft-deleted record would hold its code hostage forever.
        Assert.Contains("deleted_at IS NULL", index.GetFilter());
    }

    [Fact]
    public void SoftDeletedColumn_IsIndexed()
    {
        using var db = new TenantDbContext();

        var indexes = db.Model.FindEntityType(typeof(Branch))!.GetIndexes();

        // Every query against the entity carries "deleted_at IS NULL".
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(ISoftDeletable.DeletedAt)));
    }
}
