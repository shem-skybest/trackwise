using Trackwise.Domain.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Trackwise.Infrastructure.Persistence.Interceptors;

/// <summary>
///     Turns <c>Remove()</c> on an <see cref="ISoftDeletable" /> entity into a soft delete, so a
///     master record is never physically deleted no matter which code path asked for it.
///     <para>
///     Doing this here rather than in handlers is deliberate: master data is referenced by
///     transactions in several systems, and a hard delete anywhere would strand those references.
///     Handlers keep calling <c>Remove()</c>; the row survives and the global query filter stops
///     surfacing it.
///     </para>
///     <para>
///     Runs before <see cref="ChangeLogSaveChangesInterceptor" />, which reads the resulting
///     <c>DeletedAt</c> transition to log the deletion as a deletion rather than an edit.
///     </para>
/// </summary>
public class SoftDeleteSaveChangesInterceptor(IHttpContextAccessor? httpContextAccessor = null)
    : SaveChangesInterceptor
{
    private readonly HttpContext? httpContext = httpContextAccessor?.HttpContext;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        SoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SoftDelete(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State is not EntityState.Deleted)
            {
                continue;
            }

            // Unchanged rather than Modified: flipping straight to Modified would mark every
            // property dirty, issuing a full-row UPDATE and making the change log report a delete
            // as an edit to all columns. Only the two deletion columns actually changed.
            entry.State = EntityState.Unchanged;

            entry.Entity.DeletedAt = DateTime.UtcNow;
            entry.Entity.DeletedBy = httpContext.GetUserId();

            entry.Property(nameof(ISoftDeletable.DeletedAt)).IsModified = true;
            entry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;
        }
    }
}
