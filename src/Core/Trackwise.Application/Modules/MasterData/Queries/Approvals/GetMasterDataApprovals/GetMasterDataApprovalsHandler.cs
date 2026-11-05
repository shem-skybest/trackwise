using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetMasterDataApprovalsHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    IMasterDataPolicyProvider policies)
    : IAppRequestHandler<GetMasterDataApprovalsQuery, PagedResult<MasterDataApprovalDto>>
{
    public async Task<PagedResult<MasterDataApprovalDto>> Handle(
        GetMasterDataApprovalsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<MasterDataApprovalRequest>().Query();

        query = req.Open
            ? query.Where(r => r.Decision == null)
            : query.Where(r => r.Decision != null);

        if (!string.IsNullOrWhiteSpace(req.EntityType))
        {
            // Canonicalised through the policy table rather than compared case-insensitively in SQL:
            // an entity type arrives from a filter chip or a query string in whatever casing, and
            // the table is already the authority on what the real name is. An unknown one falls
            // through as itself and matches nothing, which is the honest answer to "show me the
            // queue for a list that does not exist".
            var entityType = policies.Find(req.EntityType)?.EntityType ?? req.EntityType.Trim();

            query = query.Where(r => r.EntityType == entityType);
        }

        var totalItems = await query.CountAsync(ct);

        // Oldest first on the open queue: the thing that has been waiting longest is the thing an
        // approver should see first, which is the opposite of every other list in the module.
        // Newest first once decided, where the question is "what just happened".
        query = req.Open
            ? query.OrderBy(r => r.SubmittedAt)
            : query.OrderByDescending(r => r.ReviewedAt);

        var requests = await query
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var me = currentUser.GetUserId()?.ToString();

        var dtos = requests
            .Select(r => new MasterDataApprovalDto
            {
                Id = r.Id,
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                EntityCode = r.EntityCode,
                EntityName = r.EntityName,
                SubmittedByName = r.SubmittedByName,
                SubmittedAt = r.SubmittedAt,
                SubmissionNote = r.SubmissionNote,
                Decision = r.Decision,
                ReviewedByName = r.ReviewedByName,
                ReviewedAt = r.ReviewedAt,
                ReviewNote = r.ReviewNote,

                // Resolved here rather than sent as an id for the client to compare: the client has
                // no business knowing other people's user ids to answer a question about itself.
                SubmittedByMe = me is not null && string.Equals(me, r.SubmittedBy, StringComparison.Ordinal)
            })
            .ToArray();

        return PagedResult<MasterDataApprovalDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
