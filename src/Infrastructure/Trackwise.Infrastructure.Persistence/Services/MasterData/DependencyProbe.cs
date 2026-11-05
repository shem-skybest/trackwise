using System.Data.Common;
using System.Text;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Infrastructure.Persistence.Services.MasterData;

/// <summary>
/// Counts the records still pointing at a master record, in one round trip, as correlated
/// subqueries hung off the record's own row.
/// <para>
/// Raw SQL for the same reason as <see cref="DuplicateProbe"/>: the dependent tables belong to
/// other aggregates that this service has no typed access to, and the context's change-tracking
/// proxies reject the sealed projection a typed <c>SqlQuery&lt;T&gt;</c> would need.
/// </para>
/// <para>
/// Counting rather than loading is deliberate. The steward is deciding whether to retire a record,
/// and "still used by 412 loads" answers that; materialising 412 rows to report a number would make
/// the probe expensive enough that somebody would want to skip it.
/// </para>
/// </summary>
internal sealed class DependencyProbe(
    TenantDbContext db,
    ITenantUnitOfWork tenantUow,
    IMasterDataPolicyProvider policies) : IDependencyProbe
{
    public async Task<DependencyProbeResult> ProbeAsync(
        DependencyProbeRequest request, CancellationToken ct = default)
    {
        if (request.Intent is MasterDataDeactivation.None)
        {
            return DependencyProbeResult.Empty;
        }

        var policy = policies.Find(request.EntityType);

        if (policy is null)
        {
            return DependencyProbeResult.Empty;
        }

        var rules = policy.DependencyRules
            .Where(rule => rule.AppliesTo.Contains(request.Intent))
            .ToList();

        // No rules means no SQL at all. Worth short-circuiting rather than issuing a query that can
        // only return zero - an entity nothing references yet is the normal case early on.
        if (rules.Count == 0)
        {
            return DependencyProbeResult.Empty;
        }

        // Activates the tenant-specific connection before any SQL runs.
        _ = tenantUow.GetCurrentTenant();

        var connection = db.Database.GetDbConnection();
        var opened = false;

        if (connection.State is not System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
            opened = true;
        }

        try
        {
            await using var command = connection.CreateCommand();
            BuildCommand(command, policy, rules, request);

            await using var reader = await command.ExecuteReaderAsync(ct);

            // No row means the record does not exist, or is already retired. Either way there is
            // nothing to stop: the handler reports "not found", and re-deactivating something
            // already inactive must not be refused just because it has dependents.
            if (!await reader.ReadAsync(ct))
            {
                return DependencyProbeResult.Empty;
            }

            var dependents = new List<DependentGroup>(rules.Count);

            for (var i = 0; i < rules.Count; i++)
            {
                dependents.Add(new DependentGroup(
                    rules[i].Label,
                    Convert.ToInt64(reader.GetValue(i)),
                    rules[i].Blocks));
            }

            return new DependencyProbeResult(dependents);
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void BuildCommand(
        DbCommand command,
        MasterDataPolicy policy,
        List<DependencyRule> rules,
        DependencyProbeRequest request)
    {
        var sql = new StringBuilder().AppendLine("SELECT");

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];

            // Table, column and live condition all come from MasterDataPolicies, which is
            // compile-time configuration. The record id is always a parameter.
            var live = string.IsNullOrWhiteSpace(rule.LiveCondition) ? "" : $" AND {rule.LiveCondition}";
            var comma = i == rules.Count - 1 ? "" : ",";

            sql.AppendLine(
                $"  (SELECT count(*) FROM {rule.TableName} WHERE {rule.Column} = @id{live}) AS c{i}{comma}");
        }

        // Hung off the master row so that a missing or already-retired record returns no row at all,
        // which is what distinguishes "nothing depends on it" from "this is not a transition".
        sql.AppendLine($"FROM {policy.TableName}")
            .AppendLine("WHERE id = @id")
            .Append(AlreadyRetiredPredicate(request.Intent));

        command.CommandText = sql.ToString();

        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = request.RecordId;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Limits the probe to a genuine transition. Deactivating a record that is already inactive, or
    /// deleting one already deleted, changes nothing - and refusing it would strand a steward who
    /// only wanted to edit a name on a record that happens to have dependents.
    /// </summary>
    private static string AlreadyRetiredPredicate(MasterDataDeactivation intent) =>
        intent is MasterDataDeactivation.Delete
            ? "  AND deleted_at IS NULL"
            : "  AND is_active AND deleted_at IS NULL";
}
