using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

/// <summary>A workfile's container slots, with type codes and box numbers resolved in two batches.</summary>
internal static class WorkfileContainerReader
{
    public static async Task<List<WorkfileContainerDto>> ReadAsync(
        ITenantUnitOfWork tenantUow, Guid workfileId, CancellationToken ct)
    {
        var rows = await tenantUow.Repository<WorkfileContainer>().Query()
            .AsNoTracking()
            .Where(c => c.WorkfileId == workfileId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            return [];
        }

        var types = await ContainerTypeLookup.ResolveAsync(
            tenantUow, rows.Select(r => (Guid?)r.ContainerTypeId), ct);

        var boxIds = rows.Select(r => r.ContainerId).OfType<Guid>().Distinct().ToArray();
        var numbers = boxIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await tenantUow.Repository<Container>().Query()
                .AsNoTracking()
                .Where(c => boxIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Number, ct);

        return rows
            .Select(r => r.ToDto(
                types.TryGetValue(r.ContainerTypeId, out var type) ? type : null,
                r.ContainerId is { } boxId ? numbers.GetValueOrDefault(boxId) : null))
            .ToList();
    }

    /// <summary>
    /// Slot id -> how people name it: the box number, or "40HC #2" (size and slot position) while
    /// the number isn't known.
    /// </summary>
    public static Dictionary<Guid, string> Labels(IReadOnlyList<WorkfileContainerDto> slots) =>
        slots.Select((s, i) => (s.Id, Label: s.ContainerNumber ?? $"{s.ContainerTypeCode} #{i + 1}"))
            .ToDictionary(s => s.Id, s => s.Label);
}
