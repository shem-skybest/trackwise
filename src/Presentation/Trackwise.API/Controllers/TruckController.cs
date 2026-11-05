using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Operations.Trucks.Queries;

namespace Trackwise.API.Controllers;

/// <summary>
/// The dispatch-facing read of a truck, for pickers and lookups that should only ever offer live
/// records.
/// <para>
/// Read-only on purpose. A truck is the same record a master-data steward maintains, and writing
/// belongs to <c>/master-data/trucks</c>, where the write goes through the duplicate and
/// dependency probes. Writing here as well would let a caller create a duplicate, rename a number
/// other systems join on, or delete past a dependency - the exact guarantees the module exists to
/// provide.
/// </para>
/// </summary>
[ApiController]
[Route("trucks")]
[Produces("application/json")]
public class TruckController(IMediator mediator) : ControllerBase
{
    [HttpGet("{truckOrDriverId:guid}", Name = "GetTruckById")]
    [ProducesResponseType(typeof(TruckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Truck.View)]
    public async Task<IActionResult> GetById(Guid truckOrDriverId, [FromQuery] GetTruckQuery request)
    {
        request.TruckOrDriverId = truckOrDriverId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetTrucks")]
    [ProducesResponseType(typeof(PagedResponse<TruckDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.Truck.View)]
    public async Task<IActionResult> GetList([FromQuery] GetTrucksQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(PagedResponse<TruckDto>.FromPagedResult(result, query.Page, query.PageSize));
    }
}
