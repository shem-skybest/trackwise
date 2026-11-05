namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// Hands out the next generated code for a master-data series, per the pattern configured for that
/// series (prefix, separator, padding).
/// <para>
/// A port rather than repository code because allocation has to be one atomic statement against the
/// tenant database. Read-then-write through the generic repository would let two stewards saving in
/// the same second take the same code, and the entity's unique index would then fail the second
/// save - which is the collision the brief's "reservation mechanism" is really asking to prevent.
/// </para>
/// </summary>
public interface IMasterDataCodeGenerator
{
    /// <summary>
    /// Reserves and returns the next code in <paramref name="series"/>, e.g. "BR-000123".
    /// <para>
    /// Creates the series' counter on first use, applying <paramref name="defaultPrefix"/> and
    /// <paramref name="defaultPadWidth"/> only at that point - an existing series keeps whatever
    /// pattern a steward configured for it. The number is consumed even if the caller's transaction
    /// later fails: gaps are acceptable, collisions are not.
    /// </para>
    /// </summary>
    /// <param name="series">Series key, conventionally the entity name (e.g. <c>Branch</c>).</param>
    /// <param name="defaultPrefix">Prefix used only when creating the series.</param>
    /// <param name="defaultPadWidth">Zero-padding width used only when creating the series.</param>
    Task<string> NextAsync(
        string series,
        string defaultPrefix = "",
        int defaultPadWidth = 6,
        CancellationToken ct = default);
}
