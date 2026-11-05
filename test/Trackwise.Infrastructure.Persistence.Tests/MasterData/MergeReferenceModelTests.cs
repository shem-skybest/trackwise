using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// A merge repoints <see cref="MasterDataPolicy.MergeReferences" /> with raw UPDATEs, so a misspelt
/// column fails a steward's merge at runtime, and a column under a unique index fails it only when
/// both records happen to have a row. Both are checked here against the EF model instead.
/// </summary>
public class MergeReferenceModelTests
{
    private static readonly IReadOnlyList<(string Entity, MergeReference Reference)> References =
        new MasterDataPolicyProvider().All
            .SelectMany(p => p.MergeReferences.Select(r => (p.EntityType, r)))
            .ToList();

    [Fact]
    public void EveryMergeReference_NamesAMappedColumn()
    {
        using var db = new TenantDbContext();

        Assert.All(References, item =>
            Assert.True(
                FindColumn(db.Model, item.Reference).Any(),
                $"{item.Entity}: {item.Reference.TableName}.{item.Reference.Column} is not in the model."));
    }

    [Fact]
    public void NoMergeReference_IsCoveredByAUniqueIndex()
    {
        using var db = new TenantDbContext();

        Assert.All(References, item =>
        {
            var table = StoreObjectIdentifier.Table(item.Reference.TableName);
            var unique = db.Model.GetEntityTypes()
                .Where(e => e.GetTableName() == item.Reference.TableName)
                .SelectMany(e => e.GetIndexes())
                .Where(i => i.IsUnique)
                .Any(i => i.Properties.Any(p => p.GetColumnName(table) == item.Reference.Column));

            Assert.False(
                unique,
                $"{item.Entity}: {item.Reference.TableName}.{item.Reference.Column} is in a unique index, "
                + "so repointing it can collide with the survivor's own row.");
        });
    }

    private static IEnumerable<IProperty> FindColumn(IModel model, MergeReference reference)
    {
        var table = StoreObjectIdentifier.Table(reference.TableName);

        // Derived types in a table-per-hierarchy map their own columns (expenses.truck_expense_truck_id),
        // so every type sharing the table is searched, not just the root.
        return model.GetEntityTypes()
            .Where(e => e.GetTableName() == reference.TableName)
            .SelectMany(e => e.GetDeclaredProperties())
            .Where(p => p.GetColumnName(table) == reference.Column);
    }
}
