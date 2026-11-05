using Trackwise.Application.Modules.MasterData.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers.MasterData;

/// <summary>
/// The approvers' queue, across every master-data entity that needs approval.
/// <para>
/// One route rather than a queue per entity, for the reason the change log is one route: an
/// approver's question is "what is waiting on me", not "what partners are waiting on me". A queue
/// per entity would also mean an approver has to know which lists need approval in order to know
/// they have finished - which is exactly the kind of thing nobody knows.
/// </para>
/// <para>
/// Reading the queue needs only <c>View</c>. Seeing what is in flight is not the same as being able
/// to decide it, and the decision routes live on the entity's own controller where the record is.
/// </para>
/// </summary>
[ApiController]
[Route("master-data/approvals")]
[Produces("application/json")]
public class MasterDataApprovalController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetMasterDataApprovals")]
    [ProducesResponseType(typeof(PagedResponse<MasterDataApprovalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetList(
        [FromQuery] GetMasterDataApprovalsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<MasterDataApprovalDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }
}
