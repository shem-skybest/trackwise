using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Maker-checker, which is step 7's whole reason for existing. These pin the two facts the rest of
/// the module leans on: nothing goes live without a second person, and that person is never the one
/// who raised it.
/// </summary>
public class MasterDataApprovalServiceTests
{
    private const string Steward = "11111111-1111-1111-1111-111111111111";
    private const string Approver = "22222222-2222-2222-2222-222222222222";

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();

    private readonly ITenantRepository<MasterDataApprovalRequest, Guid> requestRepo =
        Substitute.For<ITenantRepository<MasterDataApprovalRequest, Guid>>();

    private readonly List<MasterDataApprovalRequest> requests = [];

    private readonly BusinessPartner partner = new()
    {
        Name = "Maersk Filipinas Inc.",
        Code = "MAEU",
        Roles = [BusinessPartnerRole.ShippingLine]
    };

    public MasterDataApprovalServiceTests()
    {
        tenantUow.Repository<MasterDataApprovalRequest>().Returns(requestRepo);
        requestRepo.Query().Returns(_ => requests.BuildMock());

        // The substitute never writes anywhere, so the added row has to reach the list the query
        // reads from or every "then approve it" test would be testing an empty table.
        requestRepo
            .When(r => r.AddAsync(Arg.Any<MasterDataApprovalRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => requests.Add(call.Arg<MasterDataApprovalRequest>()));

        ActAs(Steward);
    }

    private void ActAs(string userId)
    {
        currentUser.GetUserId().Returns(Guid.Parse(userId));
        currentUser.GetUserName().Returns(userId == Steward ? "A Steward" : "An Approver");
    }

    private MasterDataApprovalService Sut() =>
        new(tenantUow, currentUser, new MasterDataPolicyProvider());

    private Task<Result> SubmitAsync(string? note = null) =>
        Sut().SubmitAsync(nameof(BusinessPartner), partner, note);

    [Fact]
    public async Task Submit_MovesADraftIntoTheQueueWithoutMakingItLive()
    {
        var result = await SubmitAsync("Details came off their BIR certificate.");

        Assert.True(result.IsSuccess);
        Assert.Equal(MasterDataApprovalState.Pending, partner.ApprovalState);

        // The point of the whole step: submitting is not publishing.
        Assert.False(partner.IsActive);

        var request = Assert.Single(requests);
        Assert.Equal(nameof(BusinessPartner), request.EntityType);
        Assert.Equal(partner.Id, request.EntityId);
        Assert.Equal(Steward, request.SubmittedBy);
        Assert.True(request.IsOpen);

        // Snapshotted, so the queue renders without joining each entity's own table.
        Assert.Equal("MAEU", request.EntityCode);
        Assert.Equal("Maersk Filipinas Inc.", request.EntityName);
    }

    [Fact]
    public async Task Submit_TwiceOver_IsRefusedRatherThanQueueingItAgain()
    {
        await SubmitAsync();

        var result = await SubmitAsync();

        Assert.False(result.IsSuccess);
        Assert.Single(requests);
    }

    [Fact]
    public async Task Approve_ByTheStewardWhoSubmittedIt_IsRefusedAndTheRecordStaysPending()
    {
        await SubmitAsync();

        var result = await Sut().ApproveAsync(nameof(BusinessPartner), partner, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.MasterDataSelfApproval, result.ErrorCode);

        // The refusal has to leave the record exactly where it was. A half-applied approval that
        // set IsActive before failing the check would be the one bug this test exists to catch.
        Assert.Equal(MasterDataApprovalState.Pending, partner.ApprovalState);
        Assert.False(partner.IsActive);
        Assert.True(requests.Single().IsOpen);
    }

    [Fact]
    public async Task Approve_BySomebodyElse_TakesTheRecordLive()
    {
        await SubmitAsync();
        ActAs(Approver);

        var result = await Sut().ApproveAsync(nameof(BusinessPartner), partner, "Checked against the TIN.");

        Assert.True(result.IsSuccess);
        Assert.Equal(MasterDataApprovalState.Approved, partner.ApprovalState);

        // Approval is the only thing in the module that sets this.
        Assert.True(partner.IsActive);

        var request = requests.Single();
        Assert.Equal(MasterDataApprovalDecision.Approved, request.Decision);
        Assert.Equal(Approver, request.ReviewedBy);
        Assert.NotNull(request.ReviewedAt);
    }

    [Fact]
    public async Task Reject_SendsTheRecordBackToDraftWithTheReasonAttached()
    {
        await SubmitAsync();
        ActAs(Approver);

        var result = await Sut().RejectAsync(nameof(BusinessPartner), partner, "TIN belongs to the parent company.");

        Assert.True(result.IsSuccess);

        // Back where it started rather than into a dead end - the steward corrects it and resubmits.
        Assert.Equal(MasterDataApprovalState.Draft, partner.ApprovalState);
        Assert.False(partner.IsActive);

        var request = requests.Single();
        Assert.Equal(MasterDataApprovalDecision.Rejected, request.Decision);
        Assert.Equal("TIN belongs to the parent company.", request.ReviewNote);
    }

    [Fact]
    public async Task Resubmitting_AfterRejection_LetsTheFirstReviewerDecideIt()
    {
        // The reason "who submitted it" lives on the request and not on the record: the check is
        // about this submission, so a second steward taking the record over frees the first
        // reviewer to approve it.
        await SubmitAsync();
        ActAs(Approver);
        await Sut().RejectAsync(nameof(BusinessPartner), partner, "Wrong address.");

        await Sut().SubmitAsync(nameof(BusinessPartner), partner, "Fixed.");
        ActAs(Steward);

        var result = await Sut().ApproveAsync(nameof(BusinessPartner), partner, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(MasterDataApprovalState.Approved, partner.ApprovalState);
    }

    [Fact]
    public async Task Approve_WithNothingSubmitted_IsRefused()
    {
        var result = await Sut().ApproveAsync(nameof(BusinessPartner), partner, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(MasterDataApprovalState.Draft, partner.ApprovalState);
        Assert.False(partner.IsActive);
    }

    [Fact]
    public async Task Submit_AnEntityWhosePolicySaysNoApproval_IsRefused()
    {
        // Approval is declared in MasterDataPolicies and nowhere else, and the guard reads the
        // entity type rather than the record - which is why an approvable record submitted under a
        // list that needs no approval is still refused. An endpoint that worked here would be a
        // second answer to a question the table already answers, and would leave request rows
        // nobody will ever look at.
        var result = await Sut().SubmitAsync(nameof(Branch), partner, null);

        Assert.False(result.IsSuccess);
        Assert.Empty(requests);
    }

    [Fact]
    public async Task Submit_WithNoIdentifiableUser_IsRefusedRatherThanApprovableByAnyone()
    {
        // Without a submitter there is nobody for the approver to differ from, which is the entire
        // guarantee. An anonymous submission anyone could approve is worse than no submission.
        currentUser.GetUserId().Returns((Guid?)null);

        var result = await SubmitAsync();

        Assert.False(result.IsSuccess);
        Assert.Empty(requests);
        Assert.Equal(MasterDataApprovalState.Draft, partner.ApprovalState);
    }
}
