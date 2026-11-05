using Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;
using Trackwise.Application.Modules.Compliance.VehiclePermits.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

/// <summary>A truck's or trailer's papers with expiry dates: LTO registration, LTFRB franchise, insurance.</summary>
[ApiController]
[Route("vehicle-permits")]
[Produces("application/json")]
public class VehiclePermitController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetVehiclePermits")]
    [ProducesResponseType(typeof(IList<VehiclePermitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Truck.View)]
    public async Task<IActionResult> GetList([FromQuery] GetVehiclePermitsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateVehiclePermit")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Truck.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateVehiclePermitCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateVehiclePermit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Truck.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehiclePermitCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{id:guid}", Name = "DeleteVehiclePermit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Truck.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteVehiclePermitCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : NotFound(ErrorResponse.FromResult(result));
    }
}
