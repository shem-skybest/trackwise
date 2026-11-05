using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Infrastructure.Persistence.Services.MasterData;

internal sealed class MasterDataCodeGenerator(TenantDbContext db, ITenantUnitOfWork tenantUow)
    : IMasterDataCodeGenerator
{
    public async Task<string> NextAsync(
        string series,
        string defaultPrefix = "",
        int defaultPadWidth = 6,
        CancellationToken ct = default)
    {
        // Activates the tenant-specific connection before any SQL runs.
        _ = tenantUow.GetCurrentTenant();

        // One statement: the counter row is created on first use, otherwise bumped, and either way
        // the allocated number is the row's new value read back from the same locked row. Splitting
        // this into a read and a write would hand the same number to two concurrent stewards.
        // FromSql/SqlQuery interpolation parameterises the arguments - never string concatenation.
        //
        // Projects a scalar, not a row shape: the context runs with change-tracking proxies, which
        // reject any sealed projection type, so SqlQuery<T> here is limited to primitives.
        // The "Value" alias is what EF Core's scalar SqlQuery<T> binds to.
        var allocated = await db.Database
            .SqlQuery<long>($"""
                INSERT INTO number_series (id, series, prefix, separator, pad_width, last_allocated, created_at)
                VALUES (gen_random_uuid(), {series}, {defaultPrefix}, '-', {defaultPadWidth}, 1, CURRENT_TIMESTAMP)
                ON CONFLICT (series)
                DO UPDATE SET last_allocated = number_series.last_allocated + 1
                RETURNING last_allocated AS "Value"
                """)
            .ToListAsync(ct);

        // Formatting is configuration, not a contended value, so reading it separately costs a
        // cheap indexed lookup and nothing in correctness - the number itself is already reserved.
        var configured = await db.Set<NumberSeries>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Series == series, ct);

        var template = configured ?? new NumberSeries
        {
            Series = series,
            Prefix = defaultPrefix,
            PadWidth = defaultPadWidth
        };

        return template.Format(allocated[0]);
    }
}
