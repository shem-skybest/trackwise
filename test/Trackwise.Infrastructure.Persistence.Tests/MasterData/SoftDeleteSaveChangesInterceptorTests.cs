using Trackwise.Domain.Core;
using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// Master data is referenced by transactions in several systems, so a physical delete anywhere
/// would strand those references. Handlers still call <c>Remove()</c>; these pin that the row
/// survives the save as an update to two columns rather than a DELETE.
/// <para>
/// Assertions read the change tracker rather than re-querying: the in-memory provider cannot
/// materialise <see cref="Branch" />'s optional <c>Address</c> complex type. That the filtered rows
/// actually disappear from queries is covered by <see cref="MasterDataModelConventionTests" />,
/// which checks the SQL the real provider emits.
/// </para>
/// </summary>
public class SoftDeleteSaveChangesInterceptorTests
{
    private static Branch NewBranch(string code = "MNL") =>
        new() { Code = code, Name = "Manila" };

    [Fact]
    public void Remove_SoftDeletableEntity_StampsDeletedAtInsteadOfDeletingTheRow()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        db.Remove(branch);
        db.SaveChanges();

        Assert.NotNull(branch.DeletedAt);

        // A real delete would leave the entry detached; the record is still there to be restored.
        Assert.Equal(EntityState.Unchanged, db.Entry(branch).State);
    }

    [Fact]
    public void Remove_SoftDeletableEntity_WritesOnlyTheDeletionColumns()
    {
        var observer = new ModifiedPropertyCapturingInterceptor();
        using var db = MasterDataSaveHarness.Create(observer);
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        db.Remove(branch);
        db.SaveChanges();

        // Flipping the entry straight to Modified would mark every property dirty, issuing a
        // full-row UPDATE and making the change log report a delete as an edit to all columns.
        Assert.Equal(
            [nameof(ISoftDeletable.DeletedAt), nameof(ISoftDeletable.DeletedBy)],
            observer.ModifiedProperties.Order().ToList());
    }

    [Fact]
    public void Remove_SoftDeletableEntity_LeavesTheRecordReadingAsWhatItWas()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        db.Remove(branch);
        db.SaveChanges();

        // A delete must not double as an edit.
        Assert.Equal("MNL", branch.Code);
        Assert.Equal("Manila", branch.Name);
        Assert.True(branch.IsActive);
    }

    [Fact]
    public void ClearingDeletedAt_RestoresTheRecord()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        db.Remove(branch);
        db.SaveChanges();

        branch.DeletedAt = null;
        branch.DeletedBy = null;
        db.SaveChanges();

        Assert.Null(branch.DeletedAt);
        Assert.Equal(EntityState.Unchanged, db.Entry(branch).State);
    }
}
