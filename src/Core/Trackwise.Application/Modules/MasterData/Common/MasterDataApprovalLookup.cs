using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Resolves the most recent approval request for a page of approvable master records, in one query.
/// <para>
/// <c>BranchNameLookup</c>'s sibling and its twin in purpose: the steward's list has to say who a
/// record is waiting on and why the last one came back, and reading that per row is a query per row.
/// Shared from the start rather than after the second copy, because rates are the next entity to
/// need exactly this.
/// </para>
/// </summary>
internal static class MasterDataApprovalLookup
{
    /// <param name="Reviewed">Null while the request is still open.</param>
    public sealed record Snapshot(
        string? SubmittedByName,
        DateTime SubmittedAt,
        DateTime? Reviewed,
        string? ReviewNote);

    public static async Task<IReadOnlyDictionary<Guid, Snapshot>> ResolveAsync(
        ITenantUnitOfWork tenantUow,
        string entityType,
        IEnumerable<Guid> recordIds,
        CancellationToken ct)
    {
        var ids = recordIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, Snapshot>();
        }

        // Newest first and grouped in memory rather than with a correlated "latest per record"
        // subquery: a record has a handful of approval rounds at most, and the alternative is SQL
        // no reader of this file would enjoy for a saving nothing measures.
        var requests = await tenantUow.Repository<MasterDataApprovalRequest>().Query()
            .Where(r => r.EntityType == entityType && ids.Contains(r.EntityId))
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(ct);

        return requests
            .GroupBy(r => r.EntityId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var latest = group.First();
                    return new Snapshot(
                        latest.SubmittedByName, latest.SubmittedAt, latest.ReviewedAt, latest.ReviewNote);
                });
    }

    /// <summary>
    /// Copies whichever half of the snapshot the record's own state makes meaningful. A pending
    /// record is waiting on someone; a draft that has been round once carries the note explaining
    /// why it came back. Neither is true of a record nobody has ever submitted.
    /// </summary>
    public static void Apply(
        Shared.Models.BusinessPartnerDto dto, IReadOnlyDictionary<Guid, Snapshot> snapshots)
    {
        if (!snapshots.TryGetValue(dto.Id, out var snapshot))
        {
            return;
        }

        if (snapshot.Reviewed is null)
        {
            dto.SubmittedByName = snapshot.SubmittedByName;
            dto.SubmittedAt = snapshot.SubmittedAt;
            return;
        }

        dto.LastReviewNote = snapshot.ReviewNote;
    }
}
