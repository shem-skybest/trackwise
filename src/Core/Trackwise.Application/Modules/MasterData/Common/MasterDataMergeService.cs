using System.Text.Json;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Core;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

internal sealed class MasterDataMergeService(
    ITenantUnitOfWork tenantUow,
    IMasterDataReferenceRepointer repointer) : IMasterDataMergeService
{
    /// <summary>
    /// A merge chain is normally one hop; anything deep is a data problem, and following it
    /// unbounded would hang on a cycle. Ten is far past anything legitimate.
    /// </summary>
    private const int MaxChainDepth = 10;

    public async Task<int> MergeAsync(
        string entityType,
        IMasterDataRecord survivor,
        IMasterDataRecord loser,
        IReadOnlyDictionary<string, string>? fieldChoices,
        string? reason,
        CancellationToken ct = default)
    {
        // The repoint below is immediate SQL while everything else is staged, so the two halves of
        // a merge only stay consistent inside one transaction. Owning it here is not an option: the
        // caller has already loaded and edited the survivor, and a retry would replay the unit
        // against a cleared change tracker and lose those edits.
        if (!tenantUow.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "A merge must run inside ITenantUnitOfWork.ExecuteInTransactionAsync. Repointing "
                + "dependent references is immediate SQL and the tombstone is staged, so outside a "
                + "transaction a failed save leaves live rows pointing at a survivor that was never "
                + "written.");
        }

        var xrefs = tenantUow.Repository<MasterDataXref>();

        // Every legacy key that resolved to the loser must now resolve to the survivor. Repointing
        // rather than deleting is the whole point: nothing any system still holds is orphaned.
        var pointingAtLoser = await xrefs.Query()
            .Where(x => x.EntityType == entityType && x.EntityId == loser.Id)
            .ToListAsync(ct);

        foreach (var xref in pointingAtLoser)
        {
            xref.EntityId = survivor.Id;
            xref.MatchNote = $"Repointed by merge of {loser.Code} into {survivor.Code}.";
        }

        // The loser's own id is a reference too - held by anything in this system that recorded it
        // before the merge - so it gets a cross-reference row of its own to forward on.
        var selfForward = await xrefs.Query()
            .FirstOrDefaultAsync(
                x => x.SourceSystem == MasterDataSourceSystem.Mdm
                     && x.SourceTable == entityType
                     && x.SourceKey == loser.Id.ToString(),
                ct);

        if (selfForward is null)
        {
            await xrefs.AddAsync(
                new MasterDataXref
                {
                    SourceSystem = MasterDataSourceSystem.Mdm,
                    SourceTable = entityType,
                    SourceKey = loser.Id.ToString(),
                    EntityType = entityType,
                    EntityId = survivor.Id,
                    MatchNote = $"Merged into {survivor.Code}."
                },
                ct);
        }
        else
        {
            selfForward.EntityId = survivor.Id;
        }

        // References held inside this system, as foreign keys, rather than by another system through
        // an xref. Branch had none; a port has loads and containers, and leaving them on the loser
        // would point them at a tombstone the soft-delete filter hides.
        var referencesRepointed = await repointer.RepointAsync(entityType, survivor.Id, loser.Id, ct);

        await tenantUow.Repository<MasterDataMergeHistory>().AddAsync(
            new MasterDataMergeHistory
            {
                EntityType = entityType,
                WinnerId = survivor.Id,
                LoserId = loser.Id,

                // Snapshotted because the loser survives only as a tombstone - without this nobody
                // can later tell what was folded in.
                LoserCode = loser.Code,
                LoserName = loser.Name,
                FieldChoices = fieldChoices is { Count: > 0 } ? JsonSerializer.Serialize(fieldChoices) : null,
                XrefsRepointed = pointingAtLoser.Count,
                ReferencesRepointed = referencesRepointed,
                Reason = reason
            },
            ct);

        loser.MergedIntoId = survivor.Id;

        // Also soft-deleted so the global query filter keeps it out of lists. The state is still
        // read as Merged, because MergedIntoId takes precedence over DeletedAt - a merged record is
        // not the same thing as one a steward deleted.
        loser.DeletedAt ??= DateTime.UtcNow;

        return pointingAtLoser.Count;
    }

    public async Task<(Guid Id, bool Forwarded)> ResolveAsync(
        string entityType, Guid id, CancellationToken ct = default)
    {
        var xrefs = tenantUow.Repository<MasterDataXref>();
        var current = id;

        for (var hop = 0; hop < MaxChainDepth; hop++)
        {
            var key = current.ToString();

            var forward = await xrefs.Query()
                .FirstOrDefaultAsync(
                    x => x.SourceSystem == MasterDataSourceSystem.Mdm
                         && x.SourceTable == entityType
                         && x.SourceKey == key,
                    ct);

            if (forward is null || forward.EntityId == current)
            {
                return (current, current != id);
            }

            current = forward.EntityId;
        }

        // Depth exhausted: return where we got to rather than throwing. A cycle is a data defect
        // that should not take a read endpoint down with it.
        return (current, current != id);
    }
}
