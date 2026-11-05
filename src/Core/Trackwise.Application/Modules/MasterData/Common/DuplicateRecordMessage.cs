using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Turns duplicate candidates into a sentence a steward can act on.
/// <para>
/// The message names the candidates because an API client that never calls the probe endpoint still
/// needs to know what it collided with. Clients that can should branch on
/// <c>ErrorCodes.DuplicateMasterDataRecord</c> and fetch the structured list from the probe.
/// </para>
/// </summary>
public static class DuplicateRecordMessage
{
    public static string For(string entityType, IReadOnlyList<DuplicateCandidate> candidates)
    {
        var described = candidates.Take(3).Select(c => $"{c.Code} - {c.Name} ({Describe(c)})");

        return $"This looks like an existing {entityType.ToLowerInvariant()}: {string.Join("; ", described)}. "
               + "Open one of those, or confirm it is genuinely different and give a reason.";
    }

    private static string Describe(DuplicateCandidate candidate)
    {
        var matched = candidate.MatchedOn.Count > 0
            ? $"matched on {string.Join(" and ", candidate.MatchedOn)}"
            : "close match";

        // Naming the state matters: "matched on name, currently deleted" tells the steward to
        // restore rather than recreate.
        return candidate.State is MasterDataState.Active
            ? matched
            : $"{matched}, currently {candidate.State.ToString().ToLowerInvariant()}";
    }
}
