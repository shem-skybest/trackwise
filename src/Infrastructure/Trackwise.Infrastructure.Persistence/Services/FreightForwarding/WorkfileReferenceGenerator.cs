using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Infrastructure.Persistence.Services.FreightForwarding;

internal sealed class WorkfileReferenceGenerator(TenantDbContext db, ITenantUnitOfWork tenantUow)
    : IWorkfileReferenceGenerator
{
    public async Task<string> NextAsync(string series, CancellationToken ct = default)
    {
        var today = Today();
        var key = WorkfileNumberSeries.CounterKey(series, today);

        // One statement: the counter is created on first use, otherwise bumped, and either way the
        // allocated number is the row's new value read back from the same locked row. Splitting
        // this into a read and a write would hand the same number to two concurrent callers.
        // FromSql/SqlQuery interpolation parameterises `key` - never string concatenation.
        // The "Value" alias is what EF Core's scalar SqlQuery<T> binds to.
        var allocated = await db.Database
            .SqlQuery<long>($"""
                INSERT INTO workfile_number_series (id, series, last_allocated)
                VALUES (gen_random_uuid(), {key}, 1)
                ON CONFLICT (series)
                DO UPDATE SET last_allocated = workfile_number_series.last_allocated + 1
                RETURNING last_allocated AS "Value"
                """)
            .ToListAsync(ct);

        return WorkfileNumberSeries.FormatReference(series, today, allocated[0]);
    }

    public async Task<ReferenceCounter> GetCounterAsync(string series, CancellationToken ct = default)
    {
        var today = Today();
        var key = WorkfileNumberSeries.CounterKey(series, today);
        var last = await db.Set<WorkfileNumberSeries>().AsNoTracking()
            .Where(s => s.Series == key)
            .Select(s => s.LastAllocated)
            .FirstOrDefaultAsync(ct);

        return new ReferenceCounter(key, last, WorkfileNumberSeries.FormatReference(series, today, last + 1));
    }

    public async Task<bool> TrySetNextAsync(string series, long nextNumber, CancellationToken ct = default)
    {
        var key = WorkfileNumberSeries.CounterKey(series, Today());
        var last = nextNumber - 1;

        // The WHERE makes the guard part of the same statement as the write, so a booking created
        // while this runs can't slip a number in between a check and the update. A refused update
        // returns no row.
        var set = await db.Database
            .SqlQuery<long>($"""
                INSERT INTO workfile_number_series (id, series, last_allocated)
                VALUES (gen_random_uuid(), {key}, {last})
                ON CONFLICT (series)
                DO UPDATE SET last_allocated = EXCLUDED.last_allocated
                WHERE workfile_number_series.last_allocated <= EXCLUDED.last_allocated
                RETURNING last_allocated AS "Value"
                """)
            .ToListAsync(ct);

        return set.Count == 1;
    }

    /// <summary>Also activates the tenant-specific connection before any SQL runs.</summary>
    private DateOnly Today() => tenantUow.GetCurrentTenant().Settings.LocalDate(DateTime.UtcNow);
}
