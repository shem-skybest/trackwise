using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// The hand-off half of the lifecycle: Open -> ManagerReview -> AccountingReview -> Closed, plus the
/// reference-closure handshake that runs orthogonally to it. The domain throws on an illegal jump,
/// so each handler has to reject one first and return a message instead.
/// </summary>
public class WorkfileLifecycleHandlersTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Workfile, Guid> workfileRepo =
        Substitute.For<ITenantRepository<Workfile, Guid>>();

    private readonly RegularWorkfile workfile;

    public WorkfileLifecycleHandlersTests()
    {
        workfile = RegularWorkfile.Create(
            "BKN-IMP-000001",
            FreightMode.Import,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });

        tenantUow.Repository<Workfile>().Returns(workfileRepo);
        workfileRepo.GetByIdAsync(workfile.Id, Arg.Any<CancellationToken>()).Returns(workfile);
    }

    private Task<Trackwise.Shared.Models.Result> Submit() =>
        new SubmitWorkfileForApprovalHandler(tenantUow)
            .Handle(new SubmitWorkfileForApprovalCommand { Id = workfile.Id }, CancellationToken.None);

    private Task<Trackwise.Shared.Models.Result> Return(string reason = "Missing BL copy") =>
        new ReturnWorkfileToOpsHandler(tenantUow)
            .Handle(new ReturnWorkfileToOpsCommand { Id = workfile.Id, Reason = reason },
                CancellationToken.None);

    private Task<Trackwise.Shared.Models.Result> Forward() =>
        new ForwardWorkfileToAccountingHandler(tenantUow)
            .Handle(new ForwardWorkfileToAccountingCommand { Id = workfile.Id }, CancellationToken.None);

    private Task<Trackwise.Shared.Models.Result> Close(string? remarks = null) =>
        new CloseWorkfileHandler(tenantUow)
            .Handle(new CloseWorkfileCommand { Id = workfile.Id, ClosedBy = "user-1", Remarks = remarks },
                CancellationToken.None);

    private Task<Trackwise.Shared.Models.Result> RequestClosure() =>
        new RequestWorkfileReferenceClosureHandler(tenantUow)
            .Handle(new RequestWorkfileReferenceClosureCommand { Id = workfile.Id },
                CancellationToken.None);

    private Task<Trackwise.Shared.Models.Result> ClearClosure(string? remarks = null) =>
        new ClearWorkfileReferenceClosureHandler(tenantUow)
            .Handle(new ClearWorkfileReferenceClosureCommand { Id = workfile.Id, Remarks = remarks },
                CancellationToken.None);

    [Fact]
    public async Task AFileWalksTheWholeChainToClosed()
    {
        Assert.True((await Submit()).IsSuccess);
        Assert.Equal(WorkfileStatus.ManagerReview, workfile.Status);

        Assert.True((await Forward()).IsSuccess);
        Assert.Equal(WorkfileStatus.AccountingReview, workfile.Status);
        Assert.True(workfile.IsForwardedToAccounting);
        Assert.NotNull(workfile.ForwardedToAccountingAt);

        Assert.True((await Close("Billed in full")).IsSuccess);
        Assert.Equal(WorkfileStatus.Closed, workfile.Status);
        Assert.Equal("user-1", workfile.ClosedBy);
        Assert.Equal("Billed in full", workfile.AccountingRemarks);
    }

    [Fact]
    public async Task ADisapprovalSendsTheFileBackToOpsAndMakesItEditableAgain()
    {
        await Submit();

        var result = await Return("Vessel name wrong");

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkfileStatus.Open, workfile.Status);
        Assert.True(workfile.IsEditable);
    }

    [Fact]
    public async Task SubmittingATwiceSubmittedFileFailsInsteadOfThrowing()
    {
        await Submit();

        var result = await Submit();

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be submitted", result.Error);
        Assert.Equal(WorkfileStatus.ManagerReview, workfile.Status);
    }

    [Fact]
    public async Task AnOpenFileCannotSkipManagerReview()
    {
        var result = await Forward();

        Assert.False(result.IsSuccess);
        Assert.Equal(WorkfileStatus.Open, workfile.Status);
        Assert.False(workfile.IsForwardedToAccounting);
    }

    [Fact]
    public async Task AFileCannotBeClosedBeforeAccountingHasIt()
    {
        await Submit();

        var result = await Close();

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be closed", result.Error);
    }

    [Fact]
    public async Task ALockedFileRefusesEveryOpsMoveButStillCloses()
    {
        await Submit();
        await Forward();
        await RequestClosure();

        var submit = await Submit();
        var returnToOps = await Return();

        Assert.False(submit.IsSuccess);
        Assert.Contains("locked by Accounting", submit.Error);
        Assert.False(returnToOps.IsSuccess);

        // Closing is the one write a locked file accepts - the lock exists so Accounting can finish.
        Assert.True((await Close()).IsSuccess);
        Assert.Equal(WorkfileStatus.Closed, workfile.Status);
    }

    [Fact]
    public async Task OpsCannotStackTwoClosureRequests()
    {
        Assert.True((await RequestClosure()).IsSuccess);
        Assert.True(workfile.IsReferenceLocked);

        var second = await RequestClosure();

        Assert.False(second.IsSuccess);
        Assert.Contains("already has a closure request", second.Error);
    }

    [Fact]
    public async Task AccountingReleasesTheLockAndCanLeaveARemark()
    {
        await RequestClosure();

        var result = await ClearClosure("Reference kept open for a re-bill");

        Assert.True(result.IsSuccess);
        Assert.False(workfile.IsReferenceLocked);
        Assert.Equal("Reference kept open for a re-bill", workfile.AccountingRemarks);
        Assert.True(workfile.IsEditable);
    }

    [Fact]
    public async Task ClearingAClosureNobodyRequestedFails()
    {
        var result = await ClearClosure();

        Assert.False(result.IsSuccess);
        Assert.Contains("no open closure request", result.Error);
    }

    [Fact]
    public async Task AClosedFileTakesNoFurtherClosureRequest()
    {
        await Submit();
        await Forward();
        await Close();

        var result = await RequestClosure();

        Assert.False(result.IsSuccess);
        Assert.Contains("nothing left to close", result.Error);
    }

    [Fact]
    public async Task AMissingFileIsReportedNotThrown()
    {
        workfileRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Workfile?)null);

        var result = await Submit();

        Assert.False(result.IsSuccess);
        Assert.Contains("Could not find workfile", result.Error);
    }
}
