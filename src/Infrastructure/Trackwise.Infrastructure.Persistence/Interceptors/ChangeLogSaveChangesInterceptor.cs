using Trackwise.Domain.Core;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Trackwise.Infrastructure.Persistence.Interceptors;

/// <summary>
///     Writes a <see cref="MasterDataChangeLog" /> row per changed field on every
///     <see cref="IMasterDataRecord" /> save.
///     <para>
///     Lives in an interceptor rather than in handlers so that a change is logged whichever path
///     made it - a steward's edit, a bulk import, a merge or an overnight ETL run. Anything that can
///     reach <c>SaveChanges</c> is covered, which is what "any master-data change is attributable to
///     a named user with a before/after value" actually requires.
///     </para>
///     <para>
///     Must run after <see cref="SoftDeleteSaveChangesInterceptor" />: it reads the
///     <c>DeletedAt</c> transition that interceptor produces to classify a delete as a delete.
///     </para>
/// </summary>
public class ChangeLogSaveChangesInterceptor(IHttpContextAccessor? httpContextAccessor = null)
    : SaveChangesInterceptor
{
    private readonly HttpContext? httpContext = httpContextAccessor?.HttpContext;

    /// <summary>
    ///     Audit columns are stamped on every save and say nothing a reader wants in a field
    ///     history - the log row carries the actor and timestamp itself.
    /// </summary>
    private static readonly HashSet<string> IgnoredProperties =
    [
        nameof(IAuditableEntity.CreatedAt),
        nameof(IAuditableEntity.CreatedBy),
        nameof(IAuditableEntity.UpdatedAt),
        nameof(IAuditableEntity.UpdatedBy)
    ];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        WriteChangeLog(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        WriteChangeLog(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteChangeLog(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var actor = httpContext.GetUserId();
        var now = DateTime.UtcNow;

        // Materialised before iterating: the loop adds log rows, which mutates the tracker.
        var entries = context.ChangeTracker.Entries<IMasterDataRecord>().ToList();
        var logs = new List<MasterDataChangeLog>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    logs.Add(Log(entry, MasterDataChangeType.Created, actor, now));
                    break;

                case EntityState.Modified:
                    logs.AddRange(LogModification(entry, actor, now));
                    break;

                // Deleted never reaches here - SoftDeleteSaveChangesInterceptor has already turned
                // it into a DeletedAt transition, which LogModification classifies.
            }
        }

        foreach (var log in logs)
        {
            context.Add(log);
        }
    }

    private static IEnumerable<MasterDataChangeLog> LogModification(
        EntityEntry<IMasterDataRecord> entry, string? actor, DateTime now)
    {
        var changed = entry.Properties
            .Where(p => p.IsModified
                        && !IgnoredProperties.Contains(p.Metadata.Name)
                        && !Equals(p.OriginalValue, p.CurrentValue))
            .ToList();

        if (changed.Count == 0)
        {
            yield break;
        }

        // A soft delete, restore or merge is one event about the whole record, not an edit to the
        // column that happens to carry it - so it is logged as such and the field rows suppressed.
        var lifecycle = ClassifyLifecycleChange(changed);

        if (lifecycle is not null)
        {
            yield return Log(entry, lifecycle.Value, actor, now);
            yield break;
        }

        foreach (var property in changed)
        {
            var log = Log(entry, MasterDataChangeType.Updated, actor, now);
            log.Field = property.Metadata.Name;
            log.OldValue = Stringify(property.OriginalValue);
            log.NewValue = Stringify(property.CurrentValue);
            yield return log;
        }
    }

    private static MasterDataChangeType? ClassifyLifecycleChange(List<PropertyEntry> changed)
    {
        // Merge is checked first, and the order is load-bearing: a merge also soft-deletes the
        // loser so it drops out of lists, so testing DeletedAt first would record every merge as an
        // ordinary deletion. Same precedence the state derivation uses.
        var mergedInto = changed.FirstOrDefault(p => p.Metadata.Name == nameof(IMasterDataRecord.MergedIntoId));

        if (mergedInto?.CurrentValue is not null)
        {
            return MasterDataChangeType.Merged;
        }

        var deletedAt = changed.FirstOrDefault(p => p.Metadata.Name == nameof(ISoftDeletable.DeletedAt));

        if (deletedAt is not null)
        {
            return deletedAt.CurrentValue is null
                ? MasterDataChangeType.Restored
                : MasterDataChangeType.Deleted;
        }

        return null;
    }

    private static MasterDataChangeLog Log(
        EntityEntry<IMasterDataRecord> entry, MasterDataChangeType changeType, string? actor, DateTime now) =>
        new()
        {
            EntityType = entry.Metadata.ClrType.Name,
            EntityId = entry.Entity.Id,
            EntityCode = entry.Entity.Code,
            ChangeType = changeType,
            ChangedBy = actor,
            ChangedAt = now
        };

    private static string? Stringify(object? value) => value switch
    {
        null => null,
        string s => s,
        DateTime d => d.ToString("O"),
        DateOnly d => d.ToString("O"),
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}
