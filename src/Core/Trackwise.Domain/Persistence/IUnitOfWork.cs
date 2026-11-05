using Trackwise.Domain.Core;

namespace Trackwise.Domain.Persistence;

/// <summary>
///     Generic Unit of Work for EF-backed aggregates. Parameterized by a marker interface
///     so derived UoWs (Tenant/Master) can constrain repositories to their domain.
/// </summary>
/// <typeparam name="TMarker">
///     Marker interface implemented by entities in this DbContext (e.g., ITenantEntity,
///     IMasterEntity).
/// </typeparam>
public interface IUnitOfWork<in TMarker> : IDisposable
{
    /// <summary>
    ///     Gets a cached repository instance for <typeparamref name="TEntity" /> with a Guid key.
    /// </summary>
    IRepository<TEntity, Guid> Repository<TEntity>()
        where TEntity : class, IEntity<Guid>, TMarker;

    /// <summary>
    ///     Gets a cached repository instance for <typeparamref name="TEntity" /> with key type <typeparamref name="TKey" />.
    /// </summary>
    IRepository<TEntity, TKey> Repository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>, TMarker;

    /// <summary>
    ///     Persists pending changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    ///     True while <see cref="ExecuteInTransactionAsync{T}" /> is running its delegate.
    /// </summary>
    /// <remarks>
    ///     For work that is only safe inside a transaction to say so out loud. A merge is the
    ///     example: it repoints dependent foreign keys with immediate SQL and stages the tombstone,
    ///     so outside a transaction a failed save leaves the references already moved.
    /// </remarks>
    bool HasActiveTransaction { get; }

    /// <summary>
    ///     Runs <paramref name="work" /> inside one transaction, saves, and commits or rolls back as
    ///     the outcome says. Nested calls join the transaction already open rather than starting a
    ///     second one.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the only way to get a transaction, and deliberately so. The configured
    ///         provider retries on failure, and a retrying execution strategy refuses a transaction
    ///         the caller began itself - the whole unit has to be handed to the strategy so a retry
    ///         can replay it as one piece. A <c>BeginTransactionAsync</c> on this interface would be
    ///         a method that throws on its first save against the real database.
    ///     </para>
    ///     <para>
    ///         A retry replays <paramref name="work" /> from the top against a cleared change
    ///         tracker, so the delegate must load every entity it means to modify itself. An entity
    ///         read before the call is detached by the replay and its edits are silently dropped.
    ///     </para>
    /// </remarks>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<TransactionOutcome<T>>> work, CancellationToken ct = default);

    /// <summary>
    ///     Executes a raw SQL command (non-query).
    /// </summary>
    Task ExecuteRawSql(string sql, CancellationToken ct = default);

    /// <summary>
    ///     Executes a raw SQL query returning a list of <typeparamref name="TSqlResponse" />.
    /// </summary>
    Task<List<TSqlResponse>> ExecuteRawSql<TSqlResponse>(string sql, CancellationToken ct = default)
        where TSqlResponse : class;

    /// <summary>
    ///     Executes an interpolated raw SQL query returning a list of <typeparamref name="TSqlResponse" />.
    /// </summary>
    Task<List<TSqlResponse>> ExecuteRawSql<TSqlResponse>(FormattableString sql, CancellationToken ct = default)
        where TSqlResponse : class;
}
