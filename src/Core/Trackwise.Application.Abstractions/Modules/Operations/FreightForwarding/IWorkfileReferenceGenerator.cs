namespace Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;

/// <summary>
/// Hands out the next reference number for a workfile series (BKN-IMP, BKN-RFV, ...).
/// <para>
/// A port rather than repository code because allocation has to be one atomic statement against the
/// tenant database. Read-then-write through the generic repository would let two files opened in
/// the same second take the same number.
/// </para>
/// </summary>
public interface IWorkfileReferenceGenerator
{
    /// <summary>
    /// Reserves and returns the next reference in <paramref name="series"/>, e.g. "BKN-IMP-000123".
    /// Creates the series' counter on first use. The number is consumed even if the caller's
    /// transaction later fails - gaps are acceptable, collisions are not.
    /// </summary>
    Task<string> NextAsync(string series, CancellationToken ct = default);

    /// <summary>Where <paramref name="series"/>'s counter stands today, without allocating.</summary>
    Task<ReferenceCounter> GetCounterAsync(string series, CancellationToken ct = default);

    /// <summary>
    /// Moves today's counter for <paramref name="series"/> so the next reference it hands out is
    /// <paramref name="nextNumber"/> - how a series continues numbering from the system it replaces.
    /// Never moves it back: returns <c>false</c> and changes nothing when a number at or past
    /// <paramref name="nextNumber"/> has already been issued, since that number would be issued twice.
    /// </summary>
    Task<bool> TrySetNextAsync(string series, long nextNumber, CancellationToken ct = default);
}

/// <param name="Key">The counter row, e.g. "BKN-RFV-26-M".</param>
/// <param name="LastAllocated">The highest number issued from it; 0 before the first.</param>
/// <param name="NextReference">The reference the next allocation will return.</param>
public sealed record ReferenceCounter(string Key, long LastAllocated, string NextReference);
