using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Abstractions.Common;

public static class UnitOfWorkTransactionExtensions
{
    /// <summary>
    /// Runs a unit of work whose answer is a <see cref="Result{T}" />: a successful result commits,
    /// a failed one rolls back.
    /// </summary>
    /// <remarks>
    /// Handlers return failures rather than throwing them, so without this the rollback is a manual
    /// step on every early return - and the one that gets forgotten commits a refusal's half-written
    /// work. As with the underlying call, the delegate must load what it edits: a retry replays it
    /// against a cleared change tracker.
    /// </remarks>
    public static Task<Result<T>> ExecuteInTransactionAsync<TMarker, T>(
        this IUnitOfWork<TMarker> uow,
        Func<CancellationToken, Task<Result<T>>> work,
        CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(
            async token =>
            {
                var result = await work(token);

                return result.IsSuccess
                    ? TransactionOutcome<Result<T>>.Keep(result)
                    : TransactionOutcome<Result<T>>.Discard(result);
            },
            ct);
    }
}
