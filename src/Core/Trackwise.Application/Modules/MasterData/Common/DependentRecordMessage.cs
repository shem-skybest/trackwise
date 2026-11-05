using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Turns blocking dependents into a sentence a steward can act on.
/// <para>
/// It names counts and groups because the useful question is not "is this in use" but "how much
/// work is reassigning it". A client that can should branch on
/// <c>ErrorCodes.MasterDataRecordInUse</c> and read the structured groups from the probe endpoint.
/// </para>
/// </summary>
public static class DependentRecordMessage
{
    public static string For(
        string entityType,
        MasterDataDeactivation intent,
        IReadOnlyList<DependentGroup> blocking)
    {
        var noun = entityType.ToLowerInvariant();
        var used = string.Join(", ", blocking.Select(d => $"{d.Count} {d.Label}"));

        // Delete and deactivate are refused for different reasons, so they get different sentences.
        // Deactivating something in use is often correct - an office closes, its history stays -
        // whereas deleting means the record should never have existed, which in-use contradicts.
        var consequence = intent is MasterDataDeactivation.Delete
            ? $"Deleting is for a {noun} that was created by mistake, and this one is in use."
            : $"They keep resolving, but this {noun} stops being offered for new work.";

        return $"This {noun} is still used by {used}. {consequence} "
               + "Reassign them first, or confirm the change and give a reason.";
    }
}
