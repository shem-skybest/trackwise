using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Infrastructure.Persistence.Services.MasterData;

/// <summary>
/// Repoints dependent foreign keys from a merged-away record to its survivor, one UPDATE per
/// declared column.
/// <para>
/// Raw SQL for the same reason as <see cref="DependencyProbe"/>: the dependent tables belong to
/// other aggregates this service has no typed access to. Set-based rather than loaded-and-tracked
/// because a busy port can carry thousands of loads, and the steward is waiting on a dialog.
/// </para>
/// </summary>
internal sealed class MasterDataReferenceRepointer(
    TenantDbContext db,
    ITenantUnitOfWork tenantUow,
    IMasterDataPolicyProvider policies) : IMasterDataReferenceRepointer
{
    public async Task<int> RepointAsync(
        string entityType,
        Guid survivorId,
        Guid loserId,
        CancellationToken ct = default)
    {
        var policy = policies.Find(entityType);

        if (policy is null)
        {
            return 0;
        }

        // One rule per (table, column); a column declared twice because it objects to deactivate
        // and delete separately must still only be updated once.
        var columns = policy.DependencyRules
            .Select(rule => (rule.TableName, rule.Column))
            .Concat(policy.MergeReferences.Select(r => (r.TableName, r.Column)))
            .Distinct()
            .ToList();

        if (columns.Count == 0)
        {
            return 0;
        }

        // Activates the tenant-specific connection before any SQL runs.
        _ = tenantUow.GetCurrentTenant();

        var repointed = 0;

        foreach (var (table, column) in columns)
        {
            // Table and column come from MasterDataPolicies, which is compile-time configuration;
            // both ids are parameters.
            var sql = $"UPDATE {table} SET {column} = {{0}} WHERE {column} = {{1}}";

            repointed += await db.Database.ExecuteSqlRawAsync(sql, [survivorId, loserId], ct);
        }

        return repointed;
    }
}
