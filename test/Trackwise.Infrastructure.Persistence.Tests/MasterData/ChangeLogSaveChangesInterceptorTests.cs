using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// "Any master-data change is attributable to a named user with a timestamp and a before/after
/// value" is an acceptance criterion, and it holds only if the log is written by the save pipeline
/// rather than by handlers - otherwise an ETL run, a merge or a bulk import writes nothing. These
/// pin the shape of what a save leaves behind.
/// </summary>
public class ChangeLogSaveChangesInterceptorTests
{
    private static Branch NewBranch(string code = "MNL") =>
        new() { Code = code, Name = "Manila" };

    private static List<MasterDataChangeLog> LogsFor(DbContext db, Branch branch) =>
        db.Set<MasterDataChangeLog>()
            .Where(l => l.EntityId == branch.Id)
            .OrderBy(l => l.Field)
            .ToList();

    [Fact]
    public void Insert_WritesASingleCreatedEntry()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();

        db.Add(branch);
        db.SaveChanges();

        var log = Assert.Single(LogsFor(db, branch));
        Assert.Equal(MasterDataChangeType.Created, log.ChangeType);
        Assert.Equal(nameof(Branch), log.EntityType);
        Assert.Equal("MNL", log.EntityCode);

        // A create has no single field to attribute - the row itself is the event.
        Assert.Null(log.Field);
    }

    [Fact]
    public void Update_WritesOneEntryPerChangedFieldWithBeforeAndAfter()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        branch.Name = "Manila Head Office";
        branch.Phone = "+63 2 8888 0000";
        db.SaveChanges();

        var updates = LogsFor(db, branch)
            .Where(l => l.ChangeType == MasterDataChangeType.Updated)
            .ToList();

        Assert.Equal(2, updates.Count);

        var name = updates.Single(l => l.Field == nameof(Branch.Name));
        Assert.Equal("Manila", name.OldValue);
        Assert.Equal("Manila Head Office", name.NewValue);

        var phone = updates.Single(l => l.Field == nameof(Branch.Phone));
        Assert.Null(phone.OldValue);
        Assert.Equal("+63 2 8888 0000", phone.NewValue);
    }

    [Fact]
    public void Update_DoesNotLogAuditColumns()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        branch.Name = "Manila Head Office";
        db.SaveChanges();

        // UpdatedAt/UpdatedBy move on every save and would bury the field the reader came for.
        var fields = LogsFor(db, branch).Select(l => l.Field).ToList();

        Assert.DoesNotContain(nameof(Branch.UpdatedAt), fields);
        Assert.DoesNotContain(nameof(Branch.UpdatedBy), fields);
        Assert.DoesNotContain(nameof(Branch.CreatedAt), fields);
    }

    [Fact]
    public void Delete_IsLoggedAsADeletionNotAsAnEditToDeletedAt()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        db.Remove(branch);
        db.SaveChanges();

        var log = Assert.Single(LogsFor(db, branch), l => l.ChangeType == MasterDataChangeType.Deleted);
        Assert.Null(log.Field);

        // The deletion is one event about the record; the columns carrying it are not news.
        Assert.DoesNotContain(LogsFor(db, branch), l => l.Field == nameof(Branch.DeletedAt));
    }

    [Fact]
    public void Restore_IsLoggedAsARestore()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        db.Remove(branch);
        db.SaveChanges();

        branch.DeletedAt = null;
        db.SaveChanges();

        Assert.Contains(LogsFor(db, branch), l => l.ChangeType == MasterDataChangeType.Restored);
    }

    [Fact]
    public void Merge_IsLoggedAsAMergeOnTheLosingRecord()
    {
        using var db = MasterDataSaveHarness.Create();
        var winner = NewBranch("MNL");
        var loser = NewBranch("MANILA");
        db.AddRange(winner, loser);
        db.SaveChanges();

        // A real merge also soft-deletes the loser so it drops out of lists. Setting only
        // MergedIntoId would pass while the interceptor recorded every actual merge as a deletion,
        // which is exactly the bug this shape caught.
        loser.MergedIntoId = winner.Id;
        loser.DeletedAt = DateTime.UtcNow;
        db.SaveChanges();

        var log = Assert.Single(LogsFor(db, loser), l => l.ChangeType == MasterDataChangeType.Merged);
        Assert.Null(log.Field);

        Assert.DoesNotContain(LogsFor(db, loser), l => l.ChangeType == MasterDataChangeType.Deleted);
    }

    [Fact]
    public void SaveWithNoRealChange_WritesNothing()
    {
        using var db = MasterDataSaveHarness.Create();
        var branch = NewBranch();
        db.Add(branch);
        db.SaveChanges();

        // Reassigning the same value must not manufacture history - stewards reopening and saving
        // a form unchanged is routine, and a log full of no-ops is a log nobody reads.
        branch.Name = "Manila";
        db.SaveChanges();

        Assert.Single(LogsFor(db, branch));
    }
}
