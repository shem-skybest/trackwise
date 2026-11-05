using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.Transactions;

/// <summary>
/// The configured provider retries on transient failure, and a retrying execution strategy refuses
/// any transaction the caller opened itself: the first save inside one throws
/// <c>"...does not support user-initiated transactions"</c>. Bulk import and merge were both written
/// against a hand-rolled <c>BeginTransactionAsync</c> and both failed on their first real database
/// call for a year, because every test substituted the unit of work - and a substitute has no
/// execution strategy and therefore no opinion.
/// <para>
/// This pins the configuration that makes <c>IUnitOfWork.ExecuteInTransactionAsync</c> the only way
/// to get a transaction. If retries are ever turned off, this test is where to come and read why
/// the wrapper exists before deciding it is no longer needed.
/// </para>
/// </summary>
public class RetryingExecutionStrategyTests
{
    [Fact]
    public void ConfiguredProvider_RetriesOnFailure_SoUserStartedTransactionsAreRefused()
    {
        // No connection is opened: choosing the strategy is a configuration question, not a
        // database one, which is what keeps this test offline.
        using var db = new TenantDbContext();

        var strategy = db.Database.CreateExecutionStrategy();

        Assert.True(
            strategy.RetriesOnFailure,
            "The tenant context no longer retries. Transactions could now be opened directly - but "
            + "every caller goes through ExecuteInTransactionAsync, so check that before changing it.");
    }
}
