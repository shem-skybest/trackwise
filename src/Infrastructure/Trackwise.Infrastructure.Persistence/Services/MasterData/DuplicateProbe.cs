using System.Data.Common;
using System.Text;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Infrastructure.Persistence.Services.MasterData;

/// <summary>
/// Finds look-alike records using PostgreSQL trigram similarity for names and equality for
/// identifiers.
/// <para>
/// Reads through a raw command rather than EF for two reasons: <c>similarity()</c> has no LINQ
/// translation, and the context runs with change-tracking proxies, which reject the sealed
/// projection type a typed <c>SqlQuery&lt;T&gt;</c> would need.
/// </para>
/// <para>
/// <b>Deliberately ignores the soft-delete query filter.</b> The most common duplicate is a record
/// somebody deleted last year - hiding it here would let the steward recreate the exact thing the
/// module exists to prevent.
/// </para>
/// </summary>
internal sealed class DuplicateProbe(
    TenantDbContext db,
    ITenantUnitOfWork tenantUow,
    IMasterDataPolicyProvider policies) : IDuplicateProbe
{
    public async Task<DuplicateProbeResult> ProbeAsync(
        DuplicateProbeRequest request, CancellationToken ct = default)
    {
        var policy = policies.Find(request.EntityType);

        if (policy is null)
        {
            return DuplicateProbeResult.Empty;
        }

        // Rules whose field the caller actually supplied a value for. Probing on a blank name would
        // match everything at score 0 and drown the steward in noise.
        var active = policy.DuplicateRules
            .Select(rule => (rule, value: Normalise(request.Fields.GetValueOrDefault(rule.Field))))
            .Where(x => x.value.Length > 0)
            .ToList();

        if (active.Count == 0)
        {
            return new DuplicateProbeResult([], policy.HardThreshold, policy.SoftThreshold);
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
            BuildCommand(command, policy, active, request);

            var candidates = new List<DuplicateCandidate>();
            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                candidates.Add(ReadCandidate(reader, active, policy));
            }

            return new DuplicateProbeResult(candidates, policy.HardThreshold, policy.SoftThreshold);
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// The index-usable half of the predicate, ORed across the active rules.
    /// <para>
    /// Scoring alone cannot use an index. <c>similarity(a, b) &gt;= 0.5</c> is a function call the
    /// planner must evaluate per row, so the trigram index sits unused and the probe reads the whole
    /// table - which is fine for five branches and is not fine for a port list. Only the <c>%</c>
    /// operator can be answered from <c>gin_trgm_ops</c>, so it goes in as a cheap pre-filter and
    /// the exact score still decides what comes back.
    /// </para>
    /// <para>
    /// Correct only while every soft threshold is at or above PostgreSQL's
    /// <c>pg_trgm.similarity_threshold</c> (0.3 by default): <c>%</c> at 0.3 returns a superset of
    /// what a 0.45 or 0.5 threshold wants. A policy below that would silently drop candidates it
    /// meant to surface, which <c>MasterDataPolicyTests</c> pins.
    /// </para>
    /// </summary>
    private static string IndexPrefilter(List<(DuplicateRule rule, string value)> active)
    {
        var predicates = active.Select((x, i) => x.rule.Kind switch
        {
            DuplicateMatchKind.Exact => $"upper(btrim({x.rule.Column})) = upper(@p{i})",
            _ => $"lower(btrim({x.rule.Column})) % lower(@p{i})"
        });

        return string.Join("\n     OR ", predicates);
    }

    private static void BuildCommand(
        DbCommand command,
        MasterDataPolicy policy,
        List<(DuplicateRule rule, string value)> active,
        DuplicateProbeRequest request)
    {
        var expressions = new string[active.Count];

        for (var i = 0; i < active.Count; i++)
        {
            var (rule, value) = active[i];

            // Column names come from MasterDataPolicies, which is compile-time configuration.
            // Values are always parameters - never interpolated.
            expressions[i] = rule.Kind switch
            {
                DuplicateMatchKind.Exact =>
                    $"(CASE WHEN upper(btrim({rule.Column})) = upper(@p{i}) THEN 1.0 ELSE 0.0 END)",
                _ => $"similarity(lower(btrim({rule.Column})), lower(@p{i}))"
            };

            AddParameter(command, $"p{i}", value);
        }

        var aliases = string.Join(", ", Enumerable.Range(0, active.Count).Select(i => $"s{i}"));
        var scored = string.Join(",\n         ", expressions.Select((e, i) => $"{e} AS s{i}"));

        // Selected for every entity so the reader's column positions are fixed, and faked for the
        // ones with no such column. The alternative - shifting every index by one for approvable
        // entities - is the kind of arithmetic that is correct until somebody adds a column.
        //
        // WARN: probing has to see drafts and pending records, exactly as it already sees deleted
        // ones. The most expensive duplicate maker-checker can produce is two stewards independently
        // raising the same partner: neither is live, so neither is visible anywhere else, and hiding
        // them here is how both get approved.
        var approvalColumn = policy.RequiresApproval ? "approval_state" : "NULL AS approval_state";

        // The inner select aliases the policy's actual code column to "code" so the outer select -
        // and every other entity's identically-shaped query - can read a fixed column name. Only
        // Truck's differs from the literal "code" (see MasterDataPolicy.CodeColumn).
        var codeColumn = policy.CodeColumn == "code" ? "code" : $"{policy.CodeColumn} AS code";

        // GREATEST over the expressions rather than the aliases: SQL cannot reference a select-list
        // alias from the same select list, hence the one subquery level.
        var sql = new StringBuilder()
            .AppendLine(
                "SELECT id, code, name, is_active, deleted_at, merged_into_id, "
                + $"approval_state, score, {aliases}")
            .AppendLine("FROM (")
            .AppendLine($"  SELECT id, {codeColumn}, name, is_active, deleted_at, merged_into_id, {approvalColumn},")
            .AppendLine($"         {scored},")
            .AppendLine($"         GREATEST({string.Join(", ", expressions)}) AS score")
            .AppendLine($"  FROM {policy.TableName}")
            .AppendLine($"  WHERE {IndexPrefilter(active)}")
            .AppendLine(") scored")
            .AppendLine("WHERE score >= @soft")
            .Append(request.ExcludeId.HasValue ? "  AND id <> @excludeId\n" : "")
            .AppendLine("ORDER BY score DESC, name")
            .AppendLine("LIMIT @limit");

        command.CommandText = sql.ToString();

        AddParameter(command, "soft", policy.SoftThreshold);
        AddParameter(command, "limit", request.Limit);

        if (request.ExcludeId.HasValue)
        {
            AddParameter(command, "excludeId", request.ExcludeId.Value);
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static DuplicateCandidate ReadCandidate(
        DbDataReader reader,
        List<(DuplicateRule rule, string value)> active,
        MasterDataPolicy policy)
    {
        var isActive = reader.GetBoolean(3);
        var deletedAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
        var mergedIntoId = reader.IsDBNull(5) ? (Guid?)null : reader.GetGuid(5);
        var approval = reader.IsDBNull(6) ? null : reader.GetString(6);
        var score = Convert.ToDouble(reader.GetValue(7));

        var matchedOn = new List<string>();

        for (var i = 0; i < active.Count; i++)
        {
            // Per-rule scores follow the aggregate score column.
            var ruleScore = Convert.ToDouble(reader.GetValue(8 + i));

            if (ruleScore >= policy.SoftThreshold)
            {
                matchedOn.Add(active[i].rule.Label);
            }
        }

        return new DuplicateCandidate(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            Math.Round(score, 4),
            matchedOn,
            MasterDataStates.Derive(isActive, deletedAt, mergedIntoId, ParseApproval(approval)));
    }

    /// <summary>
    /// Turns the stored approval column back into the enum. The column is written by
    /// <c>SnakeCaseEnumConverter</c>, so its values are lower-case; a null is the fake column an
    /// entity with no approval mode selects, which reads as "nothing was ever waiting on anybody".
    /// </summary>
    private static MasterDataApprovalState ParseApproval(string? value) =>
        Enum.TryParse<MasterDataApprovalState>(value, ignoreCase: true, out var state)
            ? state
            : MasterDataApprovalState.Approved;

    private static string Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
}
