using System.Collections.Concurrent;
using Trackwise.Domain.Core;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Trackwise.Infrastructure.Persistence;

internal abstract class UnitOfWork<TMarker>(DbContext db) : IUnitOfWork<TMarker>
{
    private readonly ConcurrentDictionary<(Type entity, Type key), object> repoCache = new();
    private IDbContextTransaction? tx;

    protected DbContext Db { get; } = db;

    public IRepository<TEntity, Guid> Repository<TEntity>()
        where TEntity : class, IEntity<Guid>, TMarker
    {
        return Repository<TEntity, Guid>();
    }

    public IRepository<TEntity, TKey> Repository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>, TMarker
    {
        var key = (typeof(TEntity), typeof(TKey));
        return (IRepository<TEntity, TKey>)repoCache.GetOrAdd(
            key,
            _ => CreateRepository<TEntity, TKey>());
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return Db.SaveChangesAsync(ct);
    }

    public bool HasActiveTransaction => tx is not null;

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<TransactionOutcome<T>>> work, CancellationToken ct = default)
    {
        // Already inside one: join it. Opening a second would either nest (which PostgreSQL will not
        // do) or commit the outer unit early, and an inner commit is the one thing that would break
        // all-or-nothing without failing.
        if (tx is not null)
        {
            return (await work(ct)).Value;
        }

        // The whole unit goes to the strategy rather than the strategy being consulted per statement:
        // a retrying strategy refuses a transaction the caller opened itself, and a retry has to
        // replay the entire unit or it replays half of one.
        PrepareConnection();
        var strategy = Db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            async token =>
            {
                // A retry re-enters here with everything the failed attempt staged still tracked,
                // which would save the abandoned attempt's work alongside the new one. Clearing is
                // also why the delegate has to load what it edits: anything read before this call is
                // detached from the second attempt onwards.
                Db.ChangeTracker.Clear();

                tx = await Db.Database.BeginTransactionAsync(token);

                try
                {
                    var outcome = await work(token);

                    if (outcome.Commit)
                    {
                        await Db.SaveChangesAsync(token);
                        await tx.CommitAsync(token);
                    }
                    else
                    {
                        await tx.RollbackAsync(token);
                    }

                    return outcome.Value;
                }
                catch
                {
                    await tx.RollbackAsync(token);
                    throw;
                }
                finally
                {
                    await tx.DisposeAsync();
                    tx = null;
                }
            },
            ct);
    }

    /// <summary>
    /// Runs before a transaction opens the connection. A connection can't be retargeted once open,
    /// so a unit of work that picks its database lazily must pick it here.
    /// </summary>
    protected virtual void PrepareConnection()
    {
    }

    public Task ExecuteRawSql(string sql, CancellationToken ct = default)
    {
        return Db.Database.ExecuteSqlRawAsync(sql, ct);
    }

    public Task<List<TSqlResponse>> ExecuteRawSql<TSqlResponse>(string sql, CancellationToken ct = default)
        where TSqlResponse : class
    {
        return Db.Set<TSqlResponse>().FromSqlRaw(sql).ToListAsync(ct);
    }

    public Task<List<TSqlResponse>> ExecuteRawSql<TSqlResponse>(FormattableString sql, CancellationToken ct = default)
        where TSqlResponse : class
    {
        return Db.Set<TSqlResponse>().FromSql(sql).ToListAsync(ct);
    }

    public void Dispose()
    {
        tx?.Dispose();
        Db.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Create a repository instance for the given entity/key types.</summary>
    protected abstract IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>, TMarker;
}
