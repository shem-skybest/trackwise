using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

public class MasterDataMergeServiceTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly IMasterDataReferenceRepointer repointer =
        Substitute.For<IMasterDataReferenceRepointer>();

    /// <summary>
    /// The half of a merge that takes effect immediately is the repoint: set-based SQL that moves
    /// live foreign keys onto the survivor. Everything else - the tombstone, the forwarding xref,
    /// the history row - is only staged. Outside a transaction a failed save therefore leaves the
    /// references already moved onto a record that was never written, and the merge cannot detect
    /// that afterwards. Refusing to start is the only place it can be caught.
    /// </summary>
    [Fact]
    public async Task MergingOutsideATransaction_IsRefusedRatherThanHalfApplied()
    {
        tenantUow.HasActiveTransaction.Returns(false);

        var sut = new MasterDataMergeService(tenantUow, repointer);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.MergeAsync(
                nameof(Branch),
                new Branch { Code = "MNL", Name = "Manila" },
                new Branch { Code = "MNL2", Name = "Manila Office" },
                null,
                "duplicate"));

        Assert.Contains("ExecuteInTransactionAsync", error.Message);
        await repointer.DidNotReceive().RepointAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
