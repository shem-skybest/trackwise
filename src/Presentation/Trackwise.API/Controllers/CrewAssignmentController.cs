using Trackwise.Application.Modules.Operations.CrewPlan.Commands;
using Trackwise.Application.Modules.Operations.CrewPlan.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

/// <summary>
/// The dispatcher's daily crew plan: per truck per day, its status and crew; and per person per day,
/// whether they're working.
/// </summary>
[ApiController]
[Route("crew-assignments")]
[Produces("application/json")]
public class CrewAssignmentController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetCrewPlan")]
    [ProducesResponseType(typeof(IList<CrewPlanRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetPlan([FromQuery] GetCrewPlanQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "SetCrewAssignment")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> Set([FromBody] SetCrewAssignmentCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("copy", Name = "CopyCrewPlan")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> Copy([FromBody] CopyCrewPlanCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("crew", Name = "GetCrewDay")]
    [ProducesResponseType(typeof(IList<CrewDayMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetCrewDay([FromQuery] GetCrewDayQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("crew/status", Name = "SetCrewStatus")]
    [ProducesResponseType(typeof(CrewStatusChangeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> SetCrewStatus([FromBody] SetCrewStatusCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{id:guid}", Name = "DeleteCrewAssignment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteCrewAssignmentCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : NotFound(ErrorResponse.FromResult(result));
    }
}
