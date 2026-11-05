using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// Finds records that still point at a master record a steward is about to deactivate or delete.
/// <para>
/// The mirror image of <see cref="IDuplicateProbe"/>, and it exists for the same reason: a
/// reference that breaks silently costs far more to find later than to be told about now. Retiring
/// the port every open booking names does not fail loudly - it leaves dropdowns that no longer
/// offer the value those bookings already hold.
/// </para>
/// <para>
/// Like the duplicate probe it decides nothing on its own. It reports what depends on the record
/// and whether that is an obstacle; a person chooses to reassign, to confirm, or to stop.
/// </para>
/// </summary>
public interface IDependencyProbe
{
    Task<DependencyProbeResult> ProbeAsync(DependencyProbeRequest request, CancellationToken ct = default);
}

/// <param name="EntityType">CLR type name, e.g. <c>Branch</c>.</param>
/// <param name="RecordId">The record being retired.</param>
/// <param name="Intent">Whether it is being deactivated or deleted - delete is held to a stricter standard.</param>
public sealed record DependencyProbeRequest(
    string EntityType,
    Guid RecordId,
    MasterDataDeactivation Intent);

/// <param name="Label">The rule's label, e.g. "trucks". Shown to the steward verbatim.</param>
/// <param name="Count">How many live rows still point at the record.</param>
/// <param name="Blocks">Whether this group stops the write until the steward confirms.</param>
public sealed record DependentGroup(string Label, long Count, bool Blocks);

public sealed record DependencyProbeResult(IReadOnlyList<DependentGroup> Dependents)
{
    /// <summary>
    /// Nothing depends on the record - or nothing needed checking, because the write is not a
    /// retirement, the entity declares no dependencies, or the record is already in that state.
    /// </summary>
    public static DependencyProbeResult Empty { get; } = new([]);

    /// <summary>Groups that stop the write until the steward confirms and gives a reason.</summary>
    public IReadOnlyList<DependentGroup> Blocking =>
        Dependents.Where(d => d.Blocks && d.Count > 0).ToList();

    public bool IsBlocked => Blocking.Count > 0;

    /// <summary>Every dependent found, advisory groups included.</summary>
    public long TotalCount => Dependents.Sum(d => d.Count);
}
