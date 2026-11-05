using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.IdentityAccess.Employees.Commands;
using Trackwise.Application.Modules.IdentityAccess.Employees.Queries;
using Trackwise.Application.Modules.Operations.Loads.Commands;
using Trackwise.Application.Modules.Operations.Trips.Commands;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("drivers")]
[Produces("application/json")]
public class DriverController(IMediator mediator) : ControllerBase
{
    [HttpGet("{userId:guid}", Name = "GetDriverByUserId")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Driver.View)]
    public async Task<IActionResult> GetById(Guid userId)
    {
        var result = await mediator.Send(new GetEmployeeByIdQuery { UserId = userId });
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpPost("{userId:guid}/device-token", Name = "SetDriverDeviceToken")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Driver.Manage)]
    public async Task<IActionResult> SetDeviceToken(Guid userId, [FromBody] SetDriverDeviceTokenCommand request)
    {
        request.UserId = userId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("confirm-load-status", Name = "ConfirmLoadStatus")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Driver.Manage)]
    public async Task<IActionResult> ConfirmLoadStatus([FromBody] ConfirmLoadStatusCommand request)
    {
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Records one of the crew's trip events from the Driver App - arrived, loading or unloading
    /// started or finished, departed, or the garage times.
    /// </summary>
    [HttpPost("trips/{tripId:guid}/events", Name = "RecordDriverTripEvent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Driver.Manage)]
    public async Task<IActionResult> RecordTripEvent(Guid tripId, [FromBody] RecordDriverTripEventCommand request)
    {
        request.TripId = tripId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Takes back the crew's own last trip event, within a few minutes.</summary>
    [HttpPost("trips/{tripId:guid}/events/undo", Name = "UndoDriverTripEvent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Driver.Manage)]
    public async Task<IActionResult> UndoTripEvent(Guid tripId, [FromBody] UndoDriverTripEventCommand request)
    {
        request.TripId = tripId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("update-load-proximity", Name = "UpdateLoadProximity")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Driver.Manage)]
    public async Task<IActionResult> UpdateLoadProximity([FromBody] UpdateLoadProximityCommand request)
    {
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
