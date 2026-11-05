using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// Booking has no status machine and accepts any transition. A workfile is handed between ops,
/// management and accounting, so an illegal jump here would skip an approval rather than just look
/// untidy - these pin every legal edge and prove the rest throw.
/// </summary>
public class WorkfileStatusMachineTests
{
    private static readonly WorkfileHeaderData Header = new() { ReportCategory = ReportCategory.Psb };

    private static RegularWorkfile NewWorkfile() =>
        RegularWorkfile.Create("BKN-IMP-000001", FreightMode.Import, Guid.NewGuid(), Guid.NewGuid(), Header);

    [Fact]
    public void ANewFileOpensEditable()
    {
        var workfile = NewWorkfile();

        Assert.Equal(WorkfileStatus.Open, workfile.Status);
        Assert.True(workfile.IsEditable);
    }

    [Theory]
    [InlineData(WorkfileStatus.Open, WorkfileStatus.ManagerReview, true)]
    [InlineData(WorkfileStatus.Open, WorkfileStatus.Cancelled, true)]
    [InlineData(WorkfileStatus.Open, WorkfileStatus.AccountingReview, false)]
    [InlineData(WorkfileStatus.Open, WorkfileStatus.Closed, false)]
    [InlineData(WorkfileStatus.ManagerReview, WorkfileStatus.Open, true)]
    [InlineData(WorkfileStatus.ManagerReview, WorkfileStatus.AccountingReview, true)]
    [InlineData(WorkfileStatus.ManagerReview, WorkfileStatus.Closed, false)]
    [InlineData(WorkfileStatus.AccountingReview, WorkfileStatus.Closed, true)]
    [InlineData(WorkfileStatus.AccountingReview, WorkfileStatus.Open, false)]
    [InlineData(WorkfileStatus.Closed, WorkfileStatus.Open, false)]
    [InlineData(WorkfileStatus.Cancelled, WorkfileStatus.Open, false)]
    public void TransitionTable(WorkfileStatus from, WorkfileStatus to, bool allowed)
    {
        Assert.Equal(allowed, WorkfileStatusMachine.CanTransition(from, to));
    }

    [Fact]
    public void SkippingManagerReview_Throws()
    {
        var workfile = NewWorkfile();

        Assert.Throws<InvalidOperationException>(() => workfile.MarkForwardedToAccounting());
    }

    [Fact]
    public void TheHappyPath_RunsOpenToClosed()
    {
        var workfile = NewWorkfile();

        workfile.SubmitForApproval();
        Assert.Equal(WorkfileStatus.ManagerReview, workfile.Status);
        Assert.False(workfile.IsEditable);

        workfile.MarkForwardedToAccounting();
        Assert.Equal(WorkfileStatus.AccountingReview, workfile.Status);
        Assert.True(workfile.IsForwardedToAccounting);
        Assert.NotNull(workfile.ForwardedToAccountingAt);

        workfile.Close("user-1");
        Assert.Equal(WorkfileStatus.Closed, workfile.Status);
        Assert.Equal("user-1", workfile.ClosedBy);
    }

    [Fact]
    public void ADisapprovalSendsTheFileBackToOps()
    {
        var workfile = NewWorkfile();
        workfile.SubmitForApproval();

        workfile.ReturnToOps("Missing packing list");

        Assert.Equal(WorkfileStatus.Open, workfile.Status);
        Assert.True(workfile.IsEditable);
    }

    [Fact]
    public void AClosedFileCannotBeCancelled()
    {
        var workfile = NewWorkfile();
        workfile.SubmitForApproval();
        workfile.MarkForwardedToAccounting();
        workfile.Close("user-1");

        Assert.Throws<InvalidOperationException>(() => workfile.Cancel("user-2", "too late"));
    }

    [Fact]
    public void AReferenceLockVetoesEditingInEveryStatus()
    {
        // Orthogonal to Status on purpose: ops opens a closure request, and until Accounting
        // releases it the file is frozen wherever it happens to sit.
        var workfile = NewWorkfile();
        Assert.True(workfile.IsEditable);

        workfile.LockReference();

        Assert.Equal(WorkfileStatus.Open, workfile.Status);
        Assert.False(workfile.IsEditable);
        Assert.Throws<InvalidOperationException>(() => workfile.UpdateHeader(Header));

        workfile.ReleaseReference();
        Assert.True(workfile.IsEditable);
    }

    [Fact]
    public void EditingAFileUnderReview_Throws()
    {
        var workfile = NewWorkfile();
        workfile.SubmitForApproval();

        Assert.Throws<InvalidOperationException>(() => workfile.UpdateHeader(Header));
    }
}
