using System.Security.Claims;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("workfiles")]
[Produces("application/json")]
public class WorkfileController(IMediator mediator) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet("{id:guid}", Name = "GetWorkfileById")]
    [ProducesResponseType(typeof(WorkfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetWorkfileByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetWorkfiles")]
    [ProducesResponseType(typeof(PagedResponse<WorkfileSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.View)]
    public async Task<IActionResult> GetList([FromQuery] GetWorkfilesQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<WorkfileSummaryDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Opens a workfile with no originating booking. Only valid for types that do not require one -
    /// in practice Reefer. Booking-backed files are promoted via
    /// <c>POST /bookings/{id}/promote-to-workfile</c>.
    /// </summary>
    [HttpPost(Name = "CreateWorkfile")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] PromoteBookingToWorkfileCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetWorkfileById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateWorkfileHeader")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateWorkfileHeaderCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Ops hands the file to a manager. <c>Open -&gt; ManagerReview</c>.
    /// </summary>
    [HttpPost("{id:guid}/submit-for-approval", Name = "SubmitWorkfileForApproval")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> SubmitForApproval(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitWorkfileForApprovalCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// The approver's disapproval. <c>ManagerReview -&gt; Open</c>, so ops can rework the file.
    /// </summary>
    [HttpPost("{id:guid}/return-to-ops", Name = "ReturnWorkfileToOps")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Approve)]
    public async Task<IActionResult> ReturnToOps(
        Guid id, [FromBody] ReturnWorkfileToOpsRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new ReturnWorkfileToOpsCommand
        {
            Id = id,
            Reason = request.Reason
        }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// The manager's sign-off. <c>ManagerReview -&gt; AccountingReview</c>.
    /// </summary>
    [HttpPost("{id:guid}/forward-to-accounting", Name = "ForwardWorkfileToAccounting")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.ForwardBilling)]
    public async Task<IActionResult> ForwardToAccounting(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ForwardWorkfileToAccountingCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Accounting's final act. <c>AccountingReview -&gt; Closed</c>.
    /// </summary>
    [HttpPost("{id:guid}/close", Name = "CloseWorkfile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Approve)]
    public async Task<IActionResult> Close(
        Guid id, [FromBody] WorkfileAccountingRemarksRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CloseWorkfileCommand
        {
            Id = id,
            ClosedBy = UserId,
            Remarks = request.Remarks
        }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Ops asks Accounting to close the reference, freezing the file until Accounting releases it.
    /// </summary>
    [HttpPost("{id:guid}/request-closure", Name = "RequestWorkfileReferenceClosure")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> RequestClosure(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RequestWorkfileReferenceClosureCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Accounting releases a locked reference. Gated by its own verb so ops cannot clear its own
    /// request - see <see cref="Permission.Workfile.ClearClosure"/>.
    /// </summary>
    [HttpPost("{id:guid}/clear-closure", Name = "ClearWorkfileReferenceClosure")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.ClearClosure)]
    public async Task<IActionResult> ClearClosure(
        Guid id, [FromBody] WorkfileAccountingRemarksRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new ClearWorkfileReferenceClosureCommand
        {
            Id = id,
            Remarks = request.Remarks
        }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Attaches or edits the genset add-on - Reefer files only.</summary>
    [HttpPut("{id:guid}/genset", Name = "SetWorkfileGensetAddOn")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> SetGensetAddOn(
        Guid id, [FromBody] SetWorkfileGensetAddOnCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Records actual genset hours as the job runs - itemized add-ons only.</summary>
    [HttpPost("{id:guid}/genset/hours", Name = "RecordWorkfileGensetHours")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> RecordGensetHours(
        Guid id, [FromBody] RecordWorkfileGensetHoursCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{id:guid}/genset", Name = "RemoveWorkfileGensetAddOn")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> RemoveGensetAddOn(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveWorkfileGensetAddOnCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Adds container slots - several of one size, or one numbered box.</summary>
    [HttpPost("{id:guid}/containers", Name = "AddWorkfileContainers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> AddContainers(
        Guid id, [FromBody] AddWorkfileContainersCommand request, CancellationToken ct)
    {
        request.WorkfileId = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}/containers/{containerId:guid}", Name = "UpdateWorkfileContainer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> UpdateContainer(
        Guid id, Guid containerId, [FromBody] UpdateWorkfileContainerCommand request, CancellationToken ct)
    {
        request.WorkfileId = id;
        request.Id = containerId;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{id:guid}/containers/{containerId:guid}", Name = "RemoveWorkfileContainer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> RemoveContainer(Guid id, Guid containerId, CancellationToken ct)
    {
        var result = await mediator.Send(
            new RemoveWorkfileContainerCommand { WorkfileId = id, Id = containerId }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Opens a truck leg - a child SLC-TRK trucking workfile with one load per container it moves.
    /// Returns the leg's id.
    /// </summary>
    [HttpPost("{id:guid}/legs", Name = "AddTruckLeg")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> AddLeg(Guid id, [FromBody] AddTruckLegCommand request, CancellationToken ct)
    {
        request.WorkfileId = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetWorkfileById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Edits a truck leg whose loads haven't gone to dispatch yet.</summary>
    [HttpPut("{id:guid}/legs/{legId:guid}", Name = "UpdateTruckLeg")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> UpdateLeg(
        Guid id, Guid legId, [FromBody] UpdateTruckLegCommand request, CancellationToken ct)
    {
        request.WorkfileId = id;
        request.LegId = legId;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/cancel", Name = "CancelWorkfile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelWorkfileRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelWorkfileCommand
        {
            Id = id,
            CancelledBy = UserId,
            Reason = request.Reason
        }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
