using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// Finds records that look like the one a steward is about to create.
/// <para>
/// The probe runs <b>before</b> the record is saved. That ordering is the module's whole economic
/// argument: stopping a duplicate at the door costs seconds, reconciling one six months later costs
/// a week. It never merges or rejects on its own - it returns candidates and the reason each
/// matched, and a person decides.
/// </para>
/// </summary>
public interface IDuplicateProbe
{
    Task<DuplicateProbeResult> ProbeAsync(DuplicateProbeRequest request, CancellationToken ct = default);
}

/// <param name="EntityType">CLR type name, e.g. <c>Branch</c>.</param>
/// <param name="Fields">Field name -> value, matching the entity's <see cref="DuplicateRule.Field"/>s.</param>
/// <param name="ExcludeId">The record being edited, so it never matches itself.</param>
/// <param name="Limit">Maximum candidates to return.</param>
public sealed record DuplicateProbeRequest(
    string EntityType,
    IReadOnlyDictionary<string, string?> Fields,
    Guid? ExcludeId = null,
    int Limit = 10);

/// <param name="Score">0-1. Exact identifier matches score 1.0.</param>
/// <param name="MatchedOn">Human-readable reasons, e.g. ["name", "TIN"].</param>
/// <param name="State">
/// Included so the steward can see a candidate is inactive or deleted. The most common duplicate is
/// one somebody deleted last year, so the probe deliberately looks past the soft-delete filter.
/// </param>
public sealed record DuplicateCandidate(
    Guid Id,
    string Code,
    string Name,
    double Score,
    IReadOnlyList<string> MatchedOn,
    MasterDataState State);

public sealed record DuplicateProbeResult(
    IReadOnlyList<DuplicateCandidate> Candidates,
    double HardThreshold,
    double SoftThreshold)
{
    public static DuplicateProbeResult Empty { get; } = new([], 1, 1);

    /// <summary>Candidates close enough that creating anyway needs an explicit declaration.</summary>
    public IReadOnlyList<DuplicateCandidate> Blocking =>
        Candidates.Where(c => c.Score >= HardThreshold).ToList();

    public bool IsBlocked => Blocking.Count > 0;
}
