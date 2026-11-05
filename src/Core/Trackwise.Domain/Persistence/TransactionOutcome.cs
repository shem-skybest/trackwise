namespace Trackwise.Domain.Persistence;

/// <summary>
///     What a transactional unit produced, and whether the work it staged should be kept.
/// </summary>
/// <remarks>
///     The delegate decides, rather than the caller deciding afterwards, because a bulk import's dry
///     run is the real work deliberately discarded: it has to run every handler and then leave
///     nothing behind, and it is not an error. Signalling that with an exception would make a
///     successful rehearsal indistinguishable from a failed one.
/// </remarks>
public readonly record struct TransactionOutcome<T>(T Value, bool Commit)
{
    /// <summary>Commit the transaction and return <paramref name="value" />.</summary>
    public static TransactionOutcome<T> Keep(T value) => new(value, true);

    /// <summary>Roll the transaction back and still return <paramref name="value" />.</summary>
    public static TransactionOutcome<T> Discard(T value) => new(value, false);
}
